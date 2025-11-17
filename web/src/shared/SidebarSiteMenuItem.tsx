import type { LucideIcon } from "lucide-react";
import { SidebarMenuButton, SidebarMenuItem } from "./ui/sidebar";
import type { MenuItem } from "./MenuItem";
import { Link } from "react-router";

type Props = {
	item: MenuItem;
};

export function SidebarSiteMenuItem({ item }: Props) {
	return <SidebarMenuItem key={item.title} >
		<SidebarMenuButton tooltip={item.title} asChild>
			<Link to={item.url}>
				{item.icon && <item.icon />}
				<span>{item.title}</span>
			</Link>
		</SidebarMenuButton>
	</SidebarMenuItem>;
}
