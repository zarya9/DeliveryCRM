// Установка HttpOnly-cookie через same-origin POST (токен кратковременно в теле запроса, в хранилище не остаётся).
window.deliveryCrmAuth = window.deliveryCrmAuth || {};

window.deliveryCrmAuth.setSessionToken = async function (token) {
    if (!token) throw new Error("empty token");
    const r = await fetch("/api/auth/session", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token: token }),
        credentials: "include"
    });
    if (!r.ok) {
        const t = await r.text();
        throw new Error(t || ("HTTP " + r.status));
    }
};

window.deliveryCrmAuth.clearSession = async function () {
    const r = await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "include"
    });
    if (!r.ok) throw new Error("logout failed");
};
