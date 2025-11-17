import { MailIcon, PlusCircleIcon, UsersIcon } from "lucide-react";

import { Button } from "@/shared/ui/button";
import {
    SidebarGroup,
    SidebarGroupContent,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/shared/ui/sidebar";
import { SidebarSiteMenuItem } from "@/shared/SidebarSiteMenuItem";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import { useEffect, useState } from "react";
import type { MenuItem } from "@/shared/MenuItem";
import { Segments } from "../routes";

export function NavMain() {
    const { role } = useAuthContext();
    const [items, setItems] = useState<MenuItem[]>([]);

    useEffect(() => {
        if (role === "admin") {
            setItems([
                // {
                //     title: "Moderators",
                //     url: Segments.moderators,
                //     icon: UsersIcon,
                // },
            ]);
        } else if (role === "user") {
            setItems([
                {
                    title: "Bands",
                    url: Segments.bands,
                    icon: UsersIcon,
                },
            ]);
        }
    }, [role]);

    return (
        <SidebarGroup>
            <SidebarGroupContent className="flex flex-col gap-2">
                <SidebarMenu>
                    <SidebarMenuItem className="flex items-center gap-2">
                        <SidebarMenuButton
                            tooltip="Quick Create"
                            className="bg-primary text-primary-foreground min-w-8 duration-200 ease-linear hover:bg-primary/90 hover:text-primary-foreground active:bg-primary/90 active:text-primary-foreground"
                        >
                            <PlusCircleIcon />
                            <span>Quick Create</span>
                        </SidebarMenuButton>
                        <Button
                            size="icon"
                            className="h-9 w-9 shrink-0 group-data-[collapsible=icon]:opacity-0"
                            variant="outline"
                        >
                            <MailIcon />
                            <span className="sr-only">Inbox</span>
                        </Button>
                    </SidebarMenuItem>
                </SidebarMenu>
                <SidebarMenu>
                    {items.map((item) => (
                        <SidebarSiteMenuItem item={item} key={item.url} />
                    ))}
                </SidebarMenu>
            </SidebarGroupContent>
        </SidebarGroup>
    );
}
