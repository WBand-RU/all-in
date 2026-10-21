"use client";

import { SidebarMenuButton, SidebarMenuItem } from "@/components/ui/sidebar";
import type { MenuItem } from "./MenuItem";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Badge } from "@/components/ui/badge";

type Props = {
    item: MenuItem;
};

export function SidebarSiteMenuItem({ item }: Props) {
    const pathname = usePathname();
    const active =
        item.url === "/app"
            ? pathname === item.url
            : pathname === item.url || pathname.startsWith(`${item.url}/`);
    return (
        <SidebarMenuItem key={item.title}>
            <SidebarMenuButton
                isActive={active}
                tooltip={item.title}
                render={
                    <Link
                        href={item.url}
                        aria-current={active ? "page" : undefined}
                        className="flex w-full items-center justify-between"
                    />
                }
            >
                <div className="flex items-center gap-2">
                    {item.icon && <item.icon />}
                    <span>{item.title}</span>
                </div>
                {!!item.badge && item.badge > 0 && (
                    <Badge
                        variant="destructive"
                        className="ml-auto h-5 px-1.5 text-xs"
                    >
                        {item.badge}
                    </Badge>
                )}
            </SidebarMenuButton>
        </SidebarMenuItem>
    );
}
