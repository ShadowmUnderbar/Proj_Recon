// RECONマスターデータ用スプレッドシート読み書きCLI
// 認証: サービスアカウント（service-account.json、gitignore対象）
// 設定: config.json の spreadsheetId
//
// 使い方:
//   node sheets-cli.mjs list-sheets
//   node sheets-cli.mjs get <シート名> [範囲(A1形式)] [--json]
//   node sheets-cli.mjs set "<シート名>!<範囲>" '<JSON 2次元配列>'
//   node sheets-cli.mjs append-rows <シート名> '<JSON 2次元配列>'
//   node sheets-cli.mjs add-column <シート名> "<変数名,型>"
//   node sheets-cli.mjs create-sheet <シート名> [--schema '<JSON 文字列配列>']
//   node sheets-cli.mjs add-enum <シート名> <数値> <日本語コメント> <要素名>
//   node sheets-cli.mjs rename-sheet <旧シート名> <新シート名>
//   node sheets-cli.mjs delete-columns <シート名> <列A1>[:<列A1>]  （例: X:Z）
//
// シート構成の前提（GASエクスポータ UpgradeDataExporter.gs と対応）:
//   - データシート: Row1=スキーマ定義行（各セル「変数名,型」、ref@シート名 で参照）、Row2以降データ
//   - enumシート: 4行目から A列=数値, B列=日本語コメント, C列=要素名

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { google } from 'googleapis';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const configPath = path.join(scriptDir, 'config.json');
const keyPath = path.join(scriptDir, 'service-account.json');

function fail(message) {
    console.error(`エラー: ${message}`);
    process.exit(1);
}

function loadConfig() {
    if (!fs.existsSync(keyPath)) {
        fail(
            `認証キーが見つかりません: ${keyPath}\n` +
            'GCPコンソールでサービスアカウントのJSONキーを発行し、上記パスに配置してください。\n' +
            '手順は .claude/skills/sheets-write/SKILL.md を参照。'
        );
    }
    let config;
    try {
        config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
    } catch (e) {
        fail(`config.json の読み込みに失敗しました: ${e.message}`);
    }
    if (!config.spreadsheetId) {
        fail(`config.json の spreadsheetId が未設定です: ${configPath}`);
    }
    return config;
}

async function createClient() {
    const auth = new google.auth.GoogleAuth({
        keyFile: keyPath,
        scopes: ['https://www.googleapis.com/auth/spreadsheets'],
    });
    return google.sheets({ version: 'v4', auth });
}

// APIエラーを日本語の対処案内つきで報告する
function explainApiError(e) {
    const status = e?.response?.status ?? e?.code;
    if (status === 403) {
        return '権限がありません(403)。スプレッドシートをサービスアカウントのメールアドレス' +
            '（service-account.json の client_email）に「編集者」で共有してください。';
    }
    if (status === 404) {
        return 'スプレッドシートが見つかりません(404)。config.json の spreadsheetId を確認してください。';
    }
    return e.message ?? String(e);
}

function parseJsonArg(raw, label) {
    try {
        return JSON.parse(raw);
    } catch (e) {
        fail(`${label} のJSONパースに失敗しました: ${e.message}\n入力値: ${raw}`);
    }
}

// get: シート範囲を読み取り TSV（既定）または JSON で表示
async function cmdGet(sheets, spreadsheetId, args, flags) {
    const [sheetName, range] = args;
    if (!sheetName) fail('使い方: get <シート名> [範囲] [--json]');
    const a1 = range ? `${sheetName}!${range}` : sheetName;
    const res = await sheets.spreadsheets.values.get({ spreadsheetId, range: a1 });
    const values = res.data.values ?? [];
    if (flags.json) {
        console.log(JSON.stringify(values, null, 2));
    } else {
        console.log(values.map(row => row.join('\t')).join('\n'));
    }
}

// list-sheets: シート名一覧
async function cmdListSheets(sheets, spreadsheetId) {
    const res = await sheets.spreadsheets.get({ spreadsheetId });
    for (const sheet of res.data.sheets) {
        const p = sheet.properties;
        console.log(`${p.title}\t(${p.gridProperties.rowCount}x${p.gridProperties.columnCount})`);
    }
}

// set: 指定範囲へ2次元配列を書き込み
async function cmdSet(sheets, spreadsheetId, args) {
    const [range, valuesJson] = args;
    if (!range || !valuesJson) fail('使い方: set "<シート名>!<範囲>" \'<JSON 2次元配列>\'');
    const values = parseJsonArg(valuesJson, '値');
    if (!Array.isArray(values) || !values.every(Array.isArray)) {
        fail('値は2次元配列で指定してください（例: [["a","b"],["c","d"]]）');
    }
    const res = await sheets.spreadsheets.values.update({
        spreadsheetId,
        range,
        valueInputOption: 'RAW',
        requestBody: { values },
    });
    console.log(`更新完了: ${res.data.updatedRange} (${res.data.updatedCells}セル)`);
}

// append-rows: データ表の末尾（A列基準）へ行を追加
async function cmdAppendRows(sheets, spreadsheetId, args) {
    const [sheetName, rowsJson] = args;
    if (!sheetName || !rowsJson) fail('使い方: append-rows <シート名> \'<JSON 2次元配列>\'');
    const rows = parseJsonArg(rowsJson, '行データ');
    if (!Array.isArray(rows) || !rows.every(Array.isArray)) {
        fail('行データは2次元配列で指定してください（例: [[13,"$Foo",9]]）');
    }
    const res = await sheets.spreadsheets.values.append({
        spreadsheetId,
        range: `${sheetName}!A:A`,
        valueInputOption: 'RAW',
        insertDataOption: 'INSERT_ROWS',
        requestBody: { values: rows },
    });
    console.log(`追加完了: ${res.data.updates.updatedRange} (${rows.length}行)`);
}

// add-column: Row1スキーマ行の右端の空きセルへ「変数名,型」を追記
async function cmdAddColumn(sheets, spreadsheetId, args) {
    const [sheetName, schemaCell] = args;
    if (!sheetName || !schemaCell) fail('使い方: add-column <シート名> "<変数名,型>"');
    if (!schemaCell.includes(',')) {
        fail(`スキーマセルは「変数名,型」形式で指定してください（例: "BuffId,string"）。入力値: ${schemaCell}`);
    }
    const res = await sheets.spreadsheets.values.get({
        spreadsheetId,
        range: `${sheetName}!1:1`,
    });
    const header = res.data.values?.[0] ?? [];
    // 右端の非空セルの次の列に書く
    let lastFilled = -1;
    header.forEach((cell, i) => {
        if (String(cell).trim() !== '') lastFilled = i;
    });
    const colIdx = lastFilled + 1;
    const colA1 = columnToA1(colIdx);
    await sheets.spreadsheets.values.update({
        spreadsheetId,
        range: `${sheetName}!${colA1}1`,
        valueInputOption: 'RAW',
        requestBody: { values: [[schemaCell]] },
    });
    console.log(`列追加完了: ${sheetName}!${colA1}1 = "${schemaCell}"`);
}

// create-sheet: シート新設（--schema 指定時はRow1にスキーマ行を設定）
async function cmdCreateSheet(sheets, spreadsheetId, args, flags) {
    const [sheetName] = args;
    if (!sheetName) fail('使い方: create-sheet <シート名> [--schema \'<JSON 文字列配列>\']');
    try {
        await sheets.spreadsheets.batchUpdate({
            spreadsheetId,
            requestBody: { requests: [{ addSheet: { properties: { title: sheetName } } }] },
        });
    } catch (e) {
        if (String(e.message).includes('already exists')) {
            fail(`シート "${sheetName}" は既に存在します。`);
        }
        throw e;
    }
    console.log(`シート作成完了: ${sheetName}`);
    if (flags.schema) {
        const schema = parseJsonArg(flags.schema, '--schema');
        if (!Array.isArray(schema) || !schema.every(s => typeof s === 'string')) {
            fail('--schema は文字列配列で指定してください（例: ["id,int","NameKey,string"]）');
        }
        await sheets.spreadsheets.values.update({
            spreadsheetId,
            range: `${sheetName}!A1`,
            valueInputOption: 'RAW',
            requestBody: { values: [schema] },
        });
        console.log(`スキーマ行設定完了: ${schema.join(' | ')}`);
    }
}

// add-enum: enumシート（4行目開始、A=数値/B=コメント/C=要素名）へ1行追記
async function cmdAddEnum(sheets, spreadsheetId, args) {
    const [sheetName, valueRaw, comment, name] = args;
    if (!sheetName || valueRaw === undefined || comment === undefined || !name) {
        fail('使い方: add-enum <シート名> <数値> <日本語コメント> <要素名>');
    }
    const value = Number(valueRaw);
    if (!Number.isInteger(value)) fail(`数値が不正です: ${valueRaw}`);

    const res = await sheets.spreadsheets.values.get({
        spreadsheetId,
        range: `${sheetName}!A4:C`,
    });
    const rows = res.data.values ?? [];
    // 既存値との重複チェック（GASエクスポータはA列が空になった時点で打ち切るため、途中の空行は不可）
    let rowCount = 0;
    for (const row of rows) {
        const cell = String(row[0] ?? '').trim();
        if (cell === '') break;
        if (Number(cell) === value) fail(`数値 ${value} は既に存在します（${row[2] ?? '?'}）`);
        if (String(row[2] ?? '').trim() === name) fail(`要素名 "${name}" は既に存在します`);
        rowCount++;
    }
    const targetRow = 4 + rowCount;
    await sheets.spreadsheets.values.update({
        spreadsheetId,
        range: `${sheetName}!A${targetRow}:C${targetRow}`,
        valueInputOption: 'RAW',
        requestBody: { values: [[value, comment, name]] },
    });
    console.log(`enum追加完了: ${sheetName} Row${targetRow} = ${name} = ${value} // ${comment}`);
}

// シートIDをタイトルから取得する
async function getSheetId(sheets, spreadsheetId, sheetName) {
    const res = await sheets.spreadsheets.get({ spreadsheetId });
    const sheet = res.data.sheets.find(s => s.properties.title === sheetName);
    if (!sheet) fail(`シート "${sheetName}" が見つかりません。`);
    return sheet.properties.sheetId;
}

// rename-sheet: シート名を変更
async function cmdRenameSheet(sheets, spreadsheetId, args) {
    const [oldName, newName] = args;
    if (!oldName || !newName) fail('使い方: rename-sheet <旧シート名> <新シート名>');
    const sheetId = await getSheetId(sheets, spreadsheetId, oldName);
    await sheets.spreadsheets.batchUpdate({
        spreadsheetId,
        requestBody: {
            requests: [{
                updateSheetProperties: {
                    properties: { sheetId, title: newName },
                    fields: 'title',
                },
            }],
        },
    });
    console.log(`シート名変更完了: ${oldName} → ${newName}`);
}

// delete-columns: 列を削除（例: "X" 単一、"X:Z" 範囲）
async function cmdDeleteColumns(sheets, spreadsheetId, args) {
    const [sheetName, colRange] = args;
    if (!sheetName || !colRange) fail('使い方: delete-columns <シート名> <列A1>[:<列A1>]');
    const [startCol, endCol = startCol] = colRange.split(':');
    const start = a1ToColumn(startCol);
    const end = a1ToColumn(endCol);
    if (start < 0 || end < start) fail(`列指定が不正です: ${colRange}`);
    const sheetId = await getSheetId(sheets, spreadsheetId, sheetName);
    await sheets.spreadsheets.batchUpdate({
        spreadsheetId,
        requestBody: {
            requests: [{
                deleteDimension: {
                    range: {
                        sheetId,
                        dimension: 'COLUMNS',
                        startIndex: start,
                        endIndex: end + 1,
                    },
                },
            }],
        },
    });
    console.log(`列削除完了: ${sheetName}!${startCol.toUpperCase()}:${endCol.toUpperCase()} (${end - start + 1}列)`);
}

// A1形式の列名を0始まりのインデックスへ変換（A→0, Z→25, AA→26）
function a1ToColumn(a1) {
    const s = String(a1).trim().toUpperCase();
    if (!/^[A-Z]+$/.test(s)) return -1;
    let n = 0;
    for (const ch of s) n = n * 26 + (ch.charCodeAt(0) - 64);
    return n - 1;
}

// 0始まりの列インデックスをA1形式の列名へ変換（0→A, 25→Z, 26→AA）
function columnToA1(index) {
    let result = '';
    let n = index;
    do {
        result = String.fromCharCode(65 + (n % 26)) + result;
        n = Math.floor(n / 26) - 1;
    } while (n >= 0);
    return result;
}

async function main() {
    const argv = process.argv.slice(2);
    const command = argv[0];
    const flags = {};
    const args = [];
    for (let i = 1; i < argv.length; i++) {
        if (argv[i] === '--json') flags.json = true;
        else if (argv[i] === '--schema') flags.schema = argv[++i];
        else args.push(argv[i]);
    }

    const commands = {
        'list-sheets': cmdListSheets,
        'get': cmdGet,
        'set': cmdSet,
        'append-rows': cmdAppendRows,
        'add-column': cmdAddColumn,
        'create-sheet': cmdCreateSheet,
        'add-enum': cmdAddEnum,
        'rename-sheet': cmdRenameSheet,
        'delete-columns': cmdDeleteColumns,
    };

    if (!command || !commands[command]) {
        console.error('使い方: node sheets-cli.mjs <コマンド> [引数...]');
        console.error(`コマンド: ${Object.keys(commands).join(', ')}`);
        process.exit(2);
    }

    const config = loadConfig();
    const sheets = await createClient();
    try {
        await commands[command](sheets, config.spreadsheetId, args, flags);
    } catch (e) {
        fail(explainApiError(e));
    }
}

await main();
