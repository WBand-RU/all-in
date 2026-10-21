import { expect, it } from "vitest";
import { eventSchema, localDateTime } from "./event-form";

it("rejects duplicate songs and invalid dates while accepting an empty setlist", () => {
    const values = {
        bandId: "12345678-1234-4234-8234-123456789012",
        title: "Концерт",
        startsAt: "2026-10-17T19:00",
        venue: "",
        description: "",
        songIds: [] as string[],
    };
    expect(eventSchema.safeParse(values).success).toBe(true);
    expect(
        eventSchema.safeParse({ ...values, startsAt: "invalid" }).success
    ).toBe(false);
    expect(
        eventSchema.safeParse({
            ...values,
            songIds: [values.bandId, values.bandId],
        }).success
    ).toBe(false);
    expect(new Date(localDateTime("2026-10-17T19:00:00Z")).toISOString()).toBe(
        "2026-10-17T19:00:00.000Z"
    );
});
