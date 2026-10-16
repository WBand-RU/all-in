import type { MixArtifact } from "./api";

/** Uses the compressed output for listening while retaining every download format. */
export function listeningMixes(artifacts: MixArtifact[]) {
    const mixes = new Map<string, MixArtifact[]>();
    for (const artifact of artifacts) {
        const key = JSON.stringify([artifact.kind, artifact.targetStemId ?? artifact.name]);
        const formats = mixes.get(key) ?? [];
        const existing = formats.findIndex(format => format.format.toLowerCase() === artifact.format.toLowerCase());
        if (existing < 0) formats.push(artifact);
        else formats[existing] = artifact;
        mixes.set(key, formats);
    }
    return [...mixes.values()].map(formats => ({
        primary: formats.find(artifact => artifact.format.toLowerCase() === "mp3") ?? formats[0],
        formats,
    }));
}
