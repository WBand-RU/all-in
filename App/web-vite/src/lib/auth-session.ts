export type { SessionResponse as SessionProfile } from "./generated-api/auth/models/sessionResponse";

let csrfToken: string | undefined;
export function setCsrfToken(token?: string) {
    csrfToken = token;
}
export function getCsrfToken() {
    return csrfToken;
}
export function login() {
    window.location.assign(`/auth/login?returnUrl=${encodeURIComponent(
        window.location.pathname + window.location.search + window.location.hash,
    )}`);
}
export function logout() {
    const form = document.createElement("form");
    form.method = "POST";
    form.action = "/auth/logout";
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = "__RequestVerificationToken";
    input.value = csrfToken ?? "";
    form.appendChild(input);
    document.body.appendChild(form);
    form.submit();
}
