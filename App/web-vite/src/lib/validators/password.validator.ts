import z from "zod";

export const PasswordValidator = z
	.string()
	.min(8)
	.max(72)
	.regex(/[A-Z]/, "A-Z required")
	.regex(/[a-z]/, "a-z required")
	.regex(/\d/, "0-9 required");