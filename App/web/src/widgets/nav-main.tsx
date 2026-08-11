import {
    UsersIcon,
    Music,
    ListMusic,
    Play,
    Maximize2,
    MessageSquare,
    Search,
    LayoutDashboard,
    MailIcon,
} from "lucide-react";

import {
    SidebarGroup,
    SidebarGroupContent,
    SidebarMenu,
} from "@/shared/ui/sidebar";
import { SidebarSiteMenuItem } from "@/shared/SidebarSiteMenuItem";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import { useEffect, useState } from "react";
import type { MenuItem } from "@/shared/MenuItem";
import { Segments } from "../routes";
// import { useGetMyInvitationsCount } from "@/lib/generated-api/band-api/user";
import { ApiCodes } from "@/lib/generated-api/band-api/models";
import { useQuery } from "@tanstack/react-query";
import { customInstance } from "@/lib/axios-instance";
// import type { ApiResponse } from "@/lib/generated-api/band-api/models";

interface CountResponse {
    code: ApiCodes;
    value: number;
}

export function NavMain() {
    useAuthContext();
    const [items, setItems] = useState<MenuItem[]>([]);

    // Get pending invitations count
    const { data: countData } = useQuery<CountResponse>({
        queryKey: ["getMyInvitationsCount"],
        queryFn: () =>
            customInstance({
                url: "/band-api/user/invitations/count",
                method: "GET",
            }),
    });
    const pendingInvitationsCount =
        countData?.code === ApiCodes.Success ? countData.value : 0;

    useEffect(() => {
        setItems([
                {
                    title: "Dashboard",
                    url: `/app`,
                    icon: LayoutDashboard,
                },
                {
                    title: "My Invitations",
                    url: `/app/${Segments.invitations}`,
                    icon: MailIcon,
                    badge: pendingInvitationsCount,
                },
                {
                    title: "Songs",
                    url: `/app/${Segments.songs}`,
                    icon: Music,
                },
                {
                    title: "Playlists",
                    url: `/app/${Segments.playlists}`,
                    icon: ListMusic,
                },
                {
                    title: "Playback",
                    url: `/app/${Segments.playback}/1`,
                    icon: Play,
                },
                {
                    title: "Stage Mode",
                    url: `/app/${Segments.stage}`,
                    icon: Maximize2,
                },
                {
                    title: "Chat",
                    url: `/app/${Segments.chat}`,
                    icon: MessageSquare,
                },
                {
                    title: "Search",
                    url: `/app/${Segments.search}`,
                    icon: Search,
                },
                {
                    title: "Bands",
                    url: `/app/${Segments.bands}`,
                    icon: UsersIcon,
                },
        ]);
    }, [pendingInvitationsCount]);

    return (
        <SidebarGroup>
            <SidebarGroupContent>
                <SidebarMenu>
                    {items.map((item) => (
                        <SidebarSiteMenuItem item={item} key={item.url} />
                    ))}
                </SidebarMenu>
            </SidebarGroupContent>
        </SidebarGroup>
    );
}
