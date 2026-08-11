export const Roles = {
    SuperAdmin: "super-admin",
    Moderator: "moderator",
} as const;

export type Roles = (typeof Roles)[keyof typeof Roles];
