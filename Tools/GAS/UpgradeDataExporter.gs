// UpgradeData CSV エクスポーター
// スプレッドシートのUpgradeDataシートからUnityインポーター向けCSVを生成する

/**
 * スプレッドシート起動時にカスタムメニューを追加
 */
function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('マスターデータ')
    .addItem('UpgradeData CSVエクスポート', 'exportUpgradeCsv')
    .addSeparator()
    .addItem('UpgradeType Enum C#エクスポート', 'exportUpgradeTypeEnumCs')
    .addItem('ConditionType Enum C#エクスポート', 'exportConditionTypeEnumCs')
    .addToUi();
}

/**
 * UpgradeTypeシートをC# enumファイルとしてエクスポート（ラッパー）
 */
function exportUpgradeTypeEnumCs() {
  exportEnumCs('UpgradeType');
}

/**
 * ConditionTypeシートをC# enumファイルとしてエクスポート（ラッパー）
 * シート構成・出力形式は UpgradeType と共通（exportEnumCs を流用）
 */
function exportConditionTypeEnumCs() {
  exportEnumCs('ConditionType');
}

/**
 * 指定シートのenum定義をC#ファイルとしてエクスポートしダウンロードダイアログを表示
 * シート列構成: A列=数値、B列=日本語コメント、C列=要素名
 * @param {string} sheetName - 対象シート名（そのままenum名・ファイル名に使用）
 */
function exportEnumCs(sheetName) {
  const ui = SpreadsheetApp.getUi();
  const ss = SpreadsheetApp.getActiveSpreadsheet();

  const sheet = ss.getSheetByName(sheetName);
  if (!sheet) {
    ui.alert('エラー', `"${sheetName}" シートが見つかりません。`, ui.ButtonSet.OK);
    return;
  }

  // 4行目（index=3）から、A列が空になるまでデータを取得
  const allData = sheet.getDataRange().getValues();
  const entries = [];
  const startRow = 3; // 0-indexed（4行目）
  for (let i = startRow; i < allData.length; i++) {
    const row = allData[i];
    if (row[0] === '' || row[0] === null || row[0] === undefined) break;

    const value   = row[0];
    const comment = row.length > 1 ? String(row[1]).trim() : '';
    const name    = row.length > 2 ? String(row[2]).trim() : '';

    if (name === '') {
      console.warn(`C列が空のためスキップ: Row${i + 1}, value=${value}`);
      continue;
    }
    entries.push({ value, comment, name });
  }

  if (entries.length === 0) {
    ui.alert('警告', 'データが0行です。空のenumを出力します。', ui.ButtonSet.OK);
  }

  const csContent = generateEnumCs(sheetName, entries);

  const template = HtmlService.createTemplateFromFile('DownloadDialog');
  template.csvContent = csContent;
  template.fileName = sheetName + '.cs';

  const html = template.evaluate()
    .setWidth(400)
    .setHeight(120)
    .setTitle('C#ファイルダウンロード');

  ui.showModalDialog(html, 'C#ファイルダウンロード');
}

/**
 * enum定義のC#ソースコード文字列を生成する
 * @param {string} sheetName - enum名
 * @param {{ value: number, comment: string, name: string }[]} entries
 * @returns {string} C#ソースコード文字列
 */
function generateEnumCs(sheetName, entries) {
  const lines = [];
  lines.push('// このファイルはGASで自動生成されました。手動編集しないでください。');
  lines.push('');
  lines.push(`public enum ${sheetName}`);
  lines.push('{');

  entries.forEach(entry => {
    const comment = entry.comment !== '' ? ` // ${entry.comment}` : '';
    lines.push(`    ${entry.name} = ${entry.value},${comment}`);
  });

  lines.push('}');
  return lines.join('\n');
}

/**
 * UpgradeDataシートをCSVとしてエクスポートしダウンロードダイアログを表示
 */
function exportUpgradeCsv() {
  const ui = SpreadsheetApp.getUi();
  const ss = SpreadsheetApp.getActiveSpreadsheet();

  const sheet = ss.getSheetByName('UpgradeData');
  if (!sheet) {
    ui.alert('エラー', '"UpgradeData" シートが見つかりません。', ui.ButtonSet.OK);
    return;
  }

  const csvContent = generateCsv(sheet, ss);
  if (csvContent === null) return; // generateCsv内でアラート済み

  const template = HtmlService.createTemplateFromFile('DownloadDialog');
  template.csvContent = csvContent;
  template.fileName = 'UpgradeData.csv';

  const html = template.evaluate()
    .setWidth(400)
    .setHeight(120)
    .setTitle('CSVダウンロード');

  ui.showModalDialog(html, 'CSVダウンロード');
}

/**
 * シートからCSV文字列を生成して返す
 * @param {GoogleAppsScript.Spreadsheet.Sheet} sheet
 * @param {GoogleAppsScript.Spreadsheet.Spreadsheet} ss
 * @returns {string|null} CSV文字列、エラー時はnull
 */
function generateCsv(sheet, ss) {
  const ui = SpreadsheetApp.getUi();
  const allData = sheet.getDataRange().getValues();

  if (allData.length === 0 || allData[0].every(cell => cell === '')) {
    ui.alert('エラー', 'Row1（スキーマ定義行）が空です。', ui.ButtonSet.OK);
    return null;
  }

  const schema = parseSchema(allData[0]);
  if (schema.length === 0) {
    ui.alert('エラー', 'スキーマを解析できませんでした。Row1を確認してください。', ui.ButtonSet.OK);
    return null;
  }

  // Row2以降のデータ行（A列が空の行まで）
  const dataRows = [];
  for (let i = 1; i < allData.length; i++) {
    if (allData[i][0] === '' || allData[i][0] === null || allData[i][0] === undefined) break;
    dataRows.push(allData[i]);
  }

  if (dataRows.length === 0) {
    ui.alert('警告', 'データが0行です。ヘッダーのみのCSVを出力します。', ui.ButtonSet.OK);
  }

  // enum バリデーション（警告のみ、エクスポートは続行）
  validateEnumValues(dataRows, schema, ss);

  // ヘッダー行
  const headerLine = schema.map(s => escapeCsvValue(s.name)).join(',');

  // データ行
  const dataLines = dataRows.map(row => {
    return schema.map(s => {
      const cell = s.colIdx < row.length ? row[s.colIdx] : '';
      return escapeCsvValue(String(cell));
    }).join(',');
  });

  return [headerLine, ...dataLines].join('\r\n');
}

/**
 * Row1のヘッダー行からスキーマ配列を生成する
 * 空セルの列はスキップし、元の列インデックスを保持する
 * @param {any[]} headerRow - スプレッドシートのRow1
 * @returns {{ name: string, type: string, colIdx: number }[]}
 */
function parseSchema(headerRow) {
  const schema = [];
  headerRow.forEach((cell, colIdx) => {
    const cellStr = String(cell).trim();
    if (cellStr === '') return; // 空セルはスキップ（出力しない）

    const parts = cellStr.split(',');
    if (parts.length < 2) {
      console.warn(`スキーマ形式が不正なセルをスキップ: "${cellStr}" (期待形式: "変数名,型")`);
      return;
    }
    schema.push({
      name: parts[0].trim(),
      type: parts[1].trim(),
      colIdx: colIdx
    });
  });
  return schema;
}

/**
 * ref@ 型の列に対してenum値のバリデーションを行う（警告のみ）
 * @param {any[][]} dataRows
 * @param {{ name: string, type: string }[]} schema
 * @param {GoogleAppsScript.Spreadsheet.Spreadsheet} ss
 */
function validateEnumValues(dataRows, schema, ss) {
  schema.forEach(col => {
    if (!col.type.startsWith('ref@')) return;

    const refSheetName = col.type.slice(4); // "ref@UpgradeType" → "UpgradeType"
    const refSheet = ss.getSheetByName(refSheetName);
    if (!refSheet) {
      console.warn(`バリデーションスキップ: 参照シート "${refSheetName}" が見つかりません。`);
      return;
    }

    // A列から有効値一覧を取得
    const refValues = refSheet.getRange('A:A').getValues()
      .flat()
      .map(v => String(v).trim())
      .filter(v => v !== '');

    dataRows.forEach((row, rowIdx) => {
      const cellValue = String(col.colIdx < row.length ? row[col.colIdx] : '').trim();
      if (!refValues.includes(cellValue)) {
        console.warn(
          `バリデーション警告: Row${rowIdx + 2}, 列"${col.name}" の値 "${cellValue}" は` +
          ` シート"${refSheetName}"に存在しません。有効値: [${refValues.join(', ')}]`
        );
      }
    });
  });
}

/**
 * RFC 4180準拠のCSVエスケープ処理
 * カンマ・ダブルクォート・改行を含む値はダブルクォートで囲む
 * @param {string} value
 * @returns {string}
 */
function escapeCsvValue(value) {
  const str = String(value);
  if (str.includes(',') || str.includes('"') || str.includes('\n') || str.includes('\r')) {
    return '"' + str.replace(/"/g, '""') + '"';
  }
  return str;
}
