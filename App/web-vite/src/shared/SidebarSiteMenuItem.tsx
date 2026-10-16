import { SidebarMenuButton, SidebarMenuItem } from "./ui/sidebar";
import type { MenuItem } from "./MenuItem";
import { Link } from "react-router";
import { Badge } from "./ui/badge";

type Props = {
    item: MenuItem;
};

export function SidebarSiteMenuItem({ item }: Props) {
    return (
        <SidebarMenuItem key={item.title}>
            <SidebarMenuButton tooltip={item.title} asChild>
                <Link
                    to={item.url}
                    className="flex items-center justify-between w-full"
                >
                    <div className="flex items-center gap-2">
                        {item.icon && <item.icon />}
                        <span>{item.title}</span>
                    </div>
                    {item.badge && item.badge > 0 && (
                        <Badge
                            variant="destructive"
                            className="ml-auto h-5 px-1.5 text-xs"
                        >
                            {item.badge}
                        </Badge>
                    )}
                </Link>
            </SidebarMenuButton>
        </SidebarMenuItem>
    );
}
