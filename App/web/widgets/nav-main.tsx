"use client";

import {
    UsersIcon,
    Music,
    LayoutDashboard,
    MailIcon,
    CalendarDays,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import {
    SidebarGroup,
    SidebarGroupContent,
    SidebarMenu,
} from "@/components/ui/sidebar";
import { SidebarSiteMenuItem } from "@/shared/SidebarSiteMenuItem";
import { useGetMyInvitationsCount } from "@/lib/generated/web-api/client";

export function NavMain() {
    const { t } = useTranslation();
    const { data } = useGetMyInvitationsCount();
    const items = [
        { title: t("nav.dashboard"), url: "/app", icon: LayoutDashboard },
        {
            title: t("nav.invitations"),
            url: "/app/invitations",
            icon: MailIcon,
            badge: Number(data?.value ?? 0),
        },
        { title: "События", url: "/app/events", icon: CalendarDays },
        { title: t("nav.songs"), url: "/app/songs", icon: Music },
        { title: t("nav.bands"), url: "/app/bands", icon: UsersIcon },
    ];
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
