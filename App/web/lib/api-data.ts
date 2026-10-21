/** Narrow an optional identifier from OpenAPI before building links or mutation requests. */
export function hasId<T extends { id?: string }>(
    value: T
): value is T & { id: string } {
    return typeof value.id === "string" && value.id.length > 0;
}
