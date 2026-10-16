import { createBrowserRouter } from "react-router";
import { HomePage } from "@/pages/HomePage";
import { AppLayout } from "@/AppLayout";
import { AuthorizedProvider } from "@/providers/AuthorizedProvider";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { BandListPage } from "@/pages/bands/BandListPage";
import { BandViewPage } from "@/pages/bands/BandViewPage";
import { SongViewPage } from "@/pages/songs/SongViewPage";
import { SongsListPage } from "@/pages/songs/SongsListPage";
import { SongFormPage } from "@/pages/songs/SongFormPage";
import { MyInvitationsPage } from "@/pages/MyInvitationsPage";

export const Segments = {
    app: "app",
    bands: "bands",
    songs: "songs",
    invitations: "invitations",
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
            {
                path: `${Segments.bands}/:id`,
                element: <BandViewPage />,
            },
            {
                path: Segments.invitations,
                element: <MyInvitationsPage />,
            },
            {
                path: Segments.songs,
                element: <SongsListPage />,
            },
            {
                path: `${Segments.songs}/:id`,
                element: <SongViewPage />,
            },
            {
                path: `${Segments.songs}/new`,
                element: <SongFormPage />,
            },
            {
                path: `${Segments.songs}/:id/edit`,
                element: <SongFormPage />,
            },
        ],
    },
    {
        path: "*",
        element: <NotFoundPage />,
    },
]);
