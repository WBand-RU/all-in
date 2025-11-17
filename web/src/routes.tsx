import { createBrowserRouter } from "react-router";
import { HomePage } from "@/pages/HomePage";
import { AppLayout } from "@/AppLayout";
import { AuthorizedProvider } from "@/providers/AuthorizedProvider";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { BandListPage } from "@/pages/BandListPage";

export const Segments = {
    app: "app",
    bands: "bands",
};

export const router = createBrowserRouter([
    {
        index: true,
        element: <HomePage />,
    },
    {
        path: "/" + Segments.app,
        element: (
            <AuthorizedProvider>
                <AppLayout />
            </AuthorizedProvider>
        ),
        children: [
            {
                index: true,
                element: <DashboardPage />,
            },
            {
                path: Segments.bands,
                element: <BandListPage />,
            },
        ],
    },
    {
        path: "*",
        element: <NotFoundPage />,
    },
]);
