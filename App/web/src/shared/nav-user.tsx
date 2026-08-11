import {
	BellIcon,
	CreditCardIcon,
	LogOutIcon,
	MoreVerticalIcon,
	UserCircleIcon,
} from "lucide-react";

import {
	Avatar,
	AvatarFallback,
	AvatarImage,
} from "@/shared/ui/avatar";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/shared/ui/dropdown-menu";
import {
	SidebarMenu,
	SidebarMenuButton,
	SidebarMenuItem,
	useSidebar,
} from "@/shared/ui/sidebar";
import { useAuthContext } from "../providers/auth/AuthorizationProviderContext";
import { Roles, type Roles as PlatformRole } from "../types/roles";
import { useKeycloak } from "@react-keycloak/web";
import { useTranslation } from "react-i18next";

export function NavUser() {
	const { isMobile } = useSidebar();
	const user = useAuthContext();
	const { keycloak } = useKeycloak();
	const { t } = useTranslation();
	const mapper = new Map<PlatformRole, string>([
		[Roles.SuperAdmin, t("user.superAdmin")],
		[Roles.Moderator, t("user.moderator")],
	]);

	if (!user.email) {
		return null;
	}

	async function handleLogOut() {
		await keycloak.logout({
			redirectUri: window.location.origin,
		});
	}

	return (
		<SidebarMenu>
			<SidebarMenuItem>
				<DropdownMenu>
					<DropdownMenuTrigger asChild>
						<SidebarMenuButton
							size="lg"
							className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
						>
							<Avatar className="h-8 w-8 rounded-lg grayscale">
								{/*<AvatarImage src={user.avatar} alt={user.name} />*/}
								<AvatarFallback className="rounded-lg">CN</AvatarFallback>
							</Avatar>
							<div className="grid flex-1 text-left text-sm leading-tight">
								<span className="truncate font-medium">{user.email}</span>
								<span className="text-muted-foreground truncate text-xs">
					{user.role ? mapper.get(user.role) : t("user.user")}
								</span>
							</div>
							<MoreVerticalIcon className="ml-auto size-4" />
						</SidebarMenuButton>
					</DropdownMenuTrigger>
					<DropdownMenuContent
						className="w-[--radix-dropdown-menu-trigger-width] min-w-56 rounded-lg"
						side={isMobile ? "bottom" : "right"}
						align="end"
						sideOffset={4}
					>
						<DropdownMenuLabel className="p-0 font-normal">
							<div className="flex items-center gap-2 px-1 py-1.5 text-left text-sm">
								<Avatar className="h-8 w-8 rounded-lg">
									<AvatarImage src={user.avatar} alt={user.name} />
									<AvatarFallback className="rounded-lg">CN</AvatarFallback>
								</Avatar>
								<div className="grid flex-1 text-left text-sm leading-tight">
									<span className="truncate font-medium">{user.name}</span>
									<span className="text-muted-foreground truncate text-xs">
										{user.email}
									</span>
								</div>
							</div>
						</DropdownMenuLabel>
						<DropdownMenuSeparator />
						<DropdownMenuGroup>
							<DropdownMenuItem>
								<UserCircleIcon />
								{t("user.account")}
							</DropdownMenuItem>
							<DropdownMenuItem>
								<CreditCardIcon />
								{t("user.billing")}
							</DropdownMenuItem>
							<DropdownMenuItem>
								<BellIcon />
								{t("user.notifications")}
							</DropdownMenuItem>
						</DropdownMenuGroup>
						<DropdownMenuSeparator />
						<DropdownMenuItem onSelect={handleLogOut}>
							<LogOutIcon />
							{t("user.logout")}
						</DropdownMenuItem>
					</DropdownMenuContent>
				</DropdownMenu>
			</SidebarMenuItem>
		</SidebarMenu>
	);
}
