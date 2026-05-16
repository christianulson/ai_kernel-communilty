import { escapeHtml } from '../utils/escapeHtml';

describe('escapeHtml', () => {
    it('ShouldEscapeAmpersand', () => {
        expect(escapeHtml('a&b')).toBe('a&amp;b');
    });

    it('ShouldEscapeLessThan', () => {
        expect(escapeHtml('<tag>')).toBe('&lt;tag&gt;');
    });

    it('ShouldEscapeDoubleQuote', () => {
        expect(escapeHtml('"quote"')).toBe('&quot;quote&quot;');
    });

    it('ShouldEscapeSingleQuote', () => {
        expect(escapeHtml("'quote'")).toBe('&#039;quote&#039;');
    });

    it('ShouldEscapeAllSpecialChars', () => {
        const input = '<script>alert("xss") & \'test\'</script>';
        const expected = '&lt;script&gt;alert(&quot;xss&quot;) &amp; &#039;test&#039;&lt;/script&gt;';
        expect(escapeHtml(input)).toBe(expected);
    });

    it('ShouldReturnEmptyString_WhenInputIsEmpty', () => {
        expect(escapeHtml('')).toBe('');
    });

    it('ShouldReturnSameString_WhenNoSpecialChars', () => {
        expect(escapeHtml('hello world 123')).toBe('hello world 123');
    });
});
