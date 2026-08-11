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
import { useTranslation } from "react-i18next";
// import type { ApiResponse } from "@/lib/generated-api/band-api/models";

interface CountResponse {
    code: ApiCodes;
    value: number;
}

export function NavMain() {
    const { t, i18n } = useTranslation();
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
                    title: t("nav.dashboard"),
                    url: `/app`,
                    icon: LayoutDashboard,
                },
                {
                    title: t("nav.invitations"),
                    url: `/app/${Segments.invitations}`,
                    icon: MailIcon,
                    badge: pendingInvitationsCount,
                },
                {
                    title: t("nav.songs"),
                    url: `/app/${Segments.songs}`,
                    icon: Music,
                },
                {
                    title: t("nav.playlists"),
                    url: `/app/${Segments.playlists}`,
                    icon: ListMusic,
                },
                {
                    title: t("nav.playback"),
                    url: `/app/${Segments.playback}/1`,
                    icon: Play,
                },
                {
                    title: t("nav.stage"),
                    url: `/app/${Segments.stage}`,
                    icon: Maximize2,
                },
                {
                    title: t("nav.chat"),
                    url: `/app/${Segments.chat}`,
                    icon: MessageSquare,
                },
                {
                    title: t("nav.search"),
                    url: `/app/${Segments.search}`,
                    icon: Search,
                },
                {
                    title: t("nav.bands"),
                    url: `/app/${Segments.bands}`,
                    icon: UsersIcon,
                },
        ]);
    }, [pendingInvitationsCount, i18n.resolvedLanguage, t]);

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
