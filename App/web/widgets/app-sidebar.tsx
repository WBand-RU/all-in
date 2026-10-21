"use client";

import * as React from "react";
import { ArrowUpCircleIcon } from "lucide-react";

import { NavMain } from "./nav-main";
import { NavUser } from "@/shared/nav-user";
import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/components/ui/sidebar";
import Link from "next/link";

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
    return (
        <Sidebar collapsible="offcanvas" {...props}>
            <SidebarHeader>
                <SidebarMenu>
                    <SidebarMenuItem>
                        <SidebarMenuButton
                            render={<Link href="/app" />}
                            className="data-[slot=sidebar-menu-button]:!p-1.5"
                        >
                            <ArrowUpCircleIcon className="h-5 w-5" />
                            <span className="text-base font-semibold">
                                WBand
                            </span>
                        </SidebarMenuButton>
                    </SidebarMenuItem>
                </SidebarMenu>
            </SidebarHeader>
            <SidebarContent>
                <NavMain />
            </SidebarContent>
            <SidebarFooter>
                <NavUser />
            </SidebarFooter>
        </Sidebar>
    );
}
