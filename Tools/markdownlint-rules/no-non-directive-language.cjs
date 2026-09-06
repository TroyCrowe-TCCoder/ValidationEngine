// Custom markdownlint-cli2 rule replacing ValidationEngine.MechanicalValidationRules.ValidateDirectiveLanguage.
// Enforces file-specification.2.1 (directive language required) by banning hedging/non-directive
// phrasing outside of quoted strings. Mirrors the banned-word regex previously implemented in
// ValidationEngine/Rules/MechanicalValidationRules.cs so both tools stay in sync until the C#
// implementation is removed (see Working/ValidationToolingRolloutPlan.md, VAL-004).

const bannedWordPattern = /\b(should|consider|it is recommended|you may|optionally)\b/gi;

module.exports = {
    names: ["global-standards-directive-language", "GS001"],
    description: "Standards files must use directive language (file-specification.2.1)",
    tags: ["standards", "language"],
    function: (params, onError) => {
        params.lines.forEach((line, lineIndex) => {
            let match;
            bannedWordPattern.lastIndex = 0;
            while ((match = bannedWordPattern.exec(line)) !== null) {
                const precedingChar = match.index > 0 ? line[match.index - 1] : "\0";
                const followingIndex = match.index + match[0].length;
                const followingChar = followingIndex < line.length ? line[followingIndex] : "\0";
                if (precedingChar === '"' && followingChar === '"') {
                    continue;
                }

                onError({
                    lineNumber: lineIndex + 1,
                    detail: `Banned non-directive word '${match[0]}' found`,
                    context: line.trim()
                });
            }
        });
    }
};
