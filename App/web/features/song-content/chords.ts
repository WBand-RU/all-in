/** Splits bracketed chord annotations for display without brackets, preserving inner whitespace. */
export function splitChords(text: string) {
    return text
        .split(/(\[[^\[\]\r\n]+\])/g)
        .filter(Boolean)
        .map((value) => {
            const chord = value.startsWith("[") && value.endsWith("]");
            return { value: chord ? value.slice(1, -1) : value, chord };
        });
}
