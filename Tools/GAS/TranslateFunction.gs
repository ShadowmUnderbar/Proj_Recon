/**
 * テキストを日本語から指定言語に翻訳するカスタム関数。
 * セル式として =TRANSLATE_JA(A1) または =TRANSLATE_JA(A1, "zh") で使用可能。
 * 先頭の $ はローカライズキーのプレフィックスとして自動除去する。
 *
 * @param {string} text 翻訳するテキスト
 * @param {string} [targetLang="en"] 翻訳先言語コード（例: "en", "zh", "ko", "fr"）
 * @return {string} 翻訳されたテキスト
 * @customfunction
 */
function TRANSLATE_JA(text, targetLang) {
  if (!text) return '';
  const cleaned = String(text).replace(/^\$/, '');
  const target = targetLang || 'en';
  return LanguageApp.translate(cleaned, 'ja', target);
}
