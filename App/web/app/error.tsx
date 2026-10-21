"use client";

export default function ErrorPage({
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <main role="alert" className="space-y-4 p-6">
            <h1 className="text-xl font-semibold">
                Не удалось загрузить страницу
            </h1>
            <p>Проверьте подключение и доступность сервисов.</p>
            <button
                type="button"
                onClick={reset}
                className="rounded border px-4 py-2"
            >
                Повторить
            </button>
        </main>
    );
}
