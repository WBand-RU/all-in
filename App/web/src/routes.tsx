import { createBrowserRouter } from "react-router";
import { HomePage } from "@/pages/HomePage";
import { AppLayout } from "@/AppLayout";
import { AuthorizedProvider } from "@/providers/AuthorizedProvider";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { BandListPage } from "@/pages/BandListPage";
import { BandViewPage } from "@/pages/BandViewPage";
import { SongViewPage } from "@/pages/songs/SongViewPage";
import { SongsListPage } from "@/pages/songs/SongsListPage";
import { SongFormPage } from "@/pages/songs/SongFormPage";
import { PlaylistsListPage } from "@/pages/playlists/PlaylistsListPage";
import { PlaylistFormPage } from "@/pages/playlists/PlaylistFormPage";
import { StagePage } from "@/pages/stage/StagePage";
import { ChatPage } from "@/pages/chat/ChatPage";
import { SearchPage } from "@/pages/search/SearchPage";
import { MyInvitationsPage } from "@/pages/MyInvitationsPage";

export const Segments = {
    app: "app",
    bands: "bands",
    songs: "songs",
    playlists: "playlists",
    stage: "stage",
    chat: "chat",
    search: "search",
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
            {
                path: Segments.playlists,
                element: <PlaylistsListPage />,
            },
            {
                path: `${Segments.playlists}/new`,
                element: <PlaylistFormPage />,
            },
            {
                path: `${Segments.playlists}/:id/edit`,
                element: <PlaylistFormPage />,
            },
            {
                path: Segments.stage,
                element: <StagePage />,
            },
            {
                path: Segments.chat,
                element: <ChatPage />,
            },
            {
                path: Segments.search,
                element: <SearchPage />,
            },
        ],
    },
    {
        path: "*",
        element: <NotFoundPage />,
    },
]);
