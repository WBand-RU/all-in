import { apiClient } from "@/lib/axios-instance";

export type FileObjectStatus = "Pending" | "Ready" | "Rejected";
export type StemKind =
    | "Backing"
    | "Vocal"
    | "Guitar"
    | "Bass"
    | "Drums"
    | "Keys"
    | "Click"
    | "Guide"
    | "Other";

export interface StemGroup {
    id: string;
    name: string;
    order: number;
}

export interface Stem {
    id: string;
    songId: string;
    groupId?: string | null;
    fileId: string;
    name: string;
    kind: StemKind;
    order: number;
    version: number;
    revision: number;
    fileStatus: FileObjectStatus;
    mimeType: string;
    size: number;
    sha256: string;
    durationMilliseconds?: number | null;
    sampleRateHz?: number | null;
    channels?: number | null;
    bitDepth?: number | null;
}

export interface StemCollection {
    songId: string;
    contentVersion: number;
    groups: StemGroup[];
    stems: Stem[];
}

export interface CreateStemInput {
    name: string;
    kind: StemKind;
    groupId?: string | null;
    fileName: string;
    mimeType: string;
    size: number;
    sha256: string;
    durationMilliseconds?: number | null;
    sampleRateHz?: number | null;
    channels?: number | null;
    bitDepth?: number | null;
}

export interface CreateStemResult {
    stem: Stem;
    uploadUrl: string;
    uploadExpiresAt: string;
    contentVersion: number;
}

export interface CreateStemBatchResult {
    files: CreateStemResult[];
    contentVersion: number;
}

export type MixStatus = "Queued" | "Processing" | "Ready" | "Failed";
export type MixKind = "Full" | "Focus" | "Minus";
export interface MixArtifact {
    id: string; kind: MixKind; targetStemId?: string | null; group: string;
    name: string; format: string; fileId: string; downloadUrl?: string | null;
}
export interface MixBatch {
    id: string; songId: string; status: MixStatus; sourceCount: number;
    error?: string | null; createdAt: string; startedAt?: string | null;
    completedAt?: string | null; heartbeatAt?: string | null; attemptCount: number;
    currentStage?: string | null; currentPlan?: string | null;
    completedOutputCount: number; totalOutputCount: number;
    artifacts: MixArtifact[];
}

export interface UpdateStemInput {
    name: string;
    kind: StemKind;
    groupId?: string | null;
    expectedRevision: number;
    durationMilliseconds?: number | null;
    sampleRateHz?: number | null;
    channels?: number | null;
    bitDepth?: number | null;
}

export interface ReorderStemItem {
    stemId: string;
    groupId?: string | null;
    order: number;
    expectedRevision: number;
}

export const stemQueryKey = (songId: string) => ["stems", songId] as const;

export async function getStems(songId: string): Promise<StemCollection> {
    return (await apiClient.get<StemCollection>(`/stem-api/songs/${songId}/stems`)).data;
}

export async function createStem(songId: string, input: CreateStemInput): Promise<CreateStemResult> {
    return (await apiClient.post<CreateStemResult>(`/stem-api/songs/${songId}/stems`, input)).data;
}

export async function createStemBatch(songId: string,
    files: CreateStemInput[]): Promise<CreateStemBatchResult> {
    return (await apiClient.post<CreateStemBatchResult>(`/stem-api/songs/${songId}/stems/batch`,
        { files })).data;
}

export async function getMixes(songId: string): Promise<MixBatch | null> {
    return (await apiClient.get<MixBatch | null>(`/mixer-api/songs/${songId}/mixes`)).data;
}

export async function queueMixes(songId: string): Promise<MixBatch> {
    return (await apiClient.post<MixBatch>(`/mixer-api/songs/${songId}/mixes`)).data;
}

export async function completeStemFile(fileId: string): Promise<void> {
    await apiClient.post(`/files/${fileId}/complete`);
}

export async function createStemGroup(songId: string, name: string, order: number): Promise<StemGroup> {
    return (await apiClient.post<StemGroup>(`/stem-api/songs/${songId}/stem-groups`, { name, order })).data;
}

export async function updateStem(stemId: string, input: UpdateStemInput): Promise<Stem> {
    return (await apiClient.put<Stem>(`/stem-api/stems/${stemId}`, input)).data;
}

export async function assignStemGroup(stemId: string, groupId: string | null,
    expectedRevision: number): Promise<Stem> {
    return (await apiClient.post<Stem>(`/stem-api/stems/${stemId}/group`, {
        groupId,
        expectedRevision,
    })).data;
}

export async function reorderStems(songId: string, expectedContentVersion: number,
    stems: ReorderStemItem[]): Promise<{ contentVersion: number }> {
    return (await apiClient.put<{ contentVersion: number }>(
        `/stem-api/songs/${songId}/stems/order`, { expectedContentVersion, stems })).data;
}

export async function getStemDownloadUrl(fileId: string): Promise<string> {
    return (await apiClient.post<{ signedUrl: string }>("/files/presign-download", { fileId })).data.signedUrl;
}

export async function sha256(file: File): Promise<string> {
    const digest = await crypto.subtle.digest("SHA-256", await file.arrayBuffer());
    return Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, "0")).join("");
}

export function uploadToSignedUrl(file: File, url: string,
    onProgress: (progress: number) => void): Promise<void> {
    return new Promise((resolve, reject) => {
        const request = new XMLHttpRequest();
        request.open("PUT", url);
        request.setRequestHeader("Content-Type", audioMimeType(file));
        request.upload.onprogress = event => {
            if (event.lengthComputable) onProgress(Math.round(event.loaded / event.total * 100));
        };
        request.onload = () => request.status >= 200 && request.status < 300
            ? resolve()
            : reject(new Error(`Upload failed with status ${request.status}`));
        request.onerror = () => reject(new Error("Upload failed"));
        request.onabort = () => reject(new Error("Upload cancelled"));
        request.send(file);
    });
}

export const supportedAudioExtensions = [".wav", ".mp3", ".ogg", ".opus", ".flac",
    ".m4a", ".aac", ".wma"];

export function isSupportedAudio(file: File): boolean {
    const name = file.name.toLowerCase();
    return supportedAudioExtensions.some(extension => name.endsWith(extension));
}

export function audioMimeType(file: File): string {
    const extension = `.${file.name.split(".").pop()?.toLowerCase()}`;
    return ({ ".wav": "audio/wav", ".mp3": "audio/mpeg", ".ogg": "audio/ogg",
        ".opus": "audio/opus", ".flac": "audio/flac", ".m4a": "audio/mp4",
        ".aac": "audio/aac", ".wma": "audio/x-ms-wma" } as Record<string, string>)[extension]
        ?? file.type ?? "application/octet-stream";
}

export function formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 ** 2) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 ** 2).toFixed(1)} MB`;
}
