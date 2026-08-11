import {
    MoreVerticalIcon,
    ShieldIcon,
    UserIcon,
    TrashIcon,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/shared/ui/dropdown-menu";
import { Badge } from "@/shared/ui/badge";
import { Avatar, AvatarFallback } from "@/shared/ui/avatar";
import { Skeleton } from "@/shared/ui/skeleton";
import {
    getGetMembersQueryKey,
    useGetMembers,
    useRemoveMember,
    useUpdateMemberRole,
} from "@/lib/generated-api/band-api/members";
import type { Member } from "@/lib/generated-api/band-api/models";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import { useQueryClient } from "@tanstack/react-query";

interface MemberCardProps {
    member: Member;
    onRoleChange: (memberId: string, newRole: "Admin" | "Member") => void;
    onRemove: (memberId: string) => void;
    canManage: boolean;
}

export function MemberCard({
    member,
    onRoleChange,
    onRemove,
    canManage,
}: MemberCardProps) {
    const canRemove = canManage && member.role !== "Owner";

    const getRoleBadgeVariant = (role: string) => {
        switch (role) {
            case "Owner":
                return "default";
            case "Admin":
                return "secondary";
            case "Member":
                return "outline";
            default:
                return "outline";
        }
    };

    const getRoleIcon = (role: string) => {
        switch (role) {
            case "Owner":
                return <ShieldIcon className="h-3 w-3" />;
            case "Admin":
                return <ShieldIcon className="h-3 w-3" />;
            case "Member":
                return <UserIcon className="h-3 w-3" />;
            default:
                return <UserIcon className="h-3 w-3" />;
        }
    };

    return (
        <div className="flex items-center justify-between p-4 border rounded-lg">
            <div className="flex items-center gap-3">
                <Avatar className="h-10 w-10">
                    <AvatarFallback>
                        {member.email.substring(0, 2).toUpperCase()}
                    </AvatarFallback>
                </Avatar>
                <div>
                    <p className="font-medium">{member.email}</p>
                    <div className="flex items-center gap-2">
                        <Badge
                            variant={getRoleBadgeVariant(member.role)}
                            className="text-xs"
                        >
                            {getRoleIcon(member.role)}
                            <span className="ml-1">
                                {member.role === "Owner"
                                    ? "Собственник"
                                    : member.role === "Admin"
                                      ? "Администратор"
                                      : "Участник"}
                            </span>
                        </Badge>
                        <span className="text-xs text-muted-foreground">
                            Joined{" "}
                            {new Date(member.joinedAt).toLocaleDateString()}
                        </span>
                    </div>
                </div>
            </div>

            {canManage && (
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm">
                            <MoreVerticalIcon className="h-4 w-4" />
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                        {member.role !== "Owner" && (
                            <>
                                {member.role !== "Admin" && (
                                    <DropdownMenuItem
                                        onClick={() =>
                                            onRoleChange(member.id, "Admin")
                                        }
                                    >
                                        <ShieldIcon className="h-4 w-4 mr-2" />
                                        Make Admin
                                    </DropdownMenuItem>
                                )}
                                {member.role === "Admin" && (
                                    <DropdownMenuItem
                                        onClick={() =>
                                            onRoleChange(member.id, "Member")
                                        }
                                    >
                                        <UserIcon className="h-4 w-4 mr-2" />
                                        Make Member
                                    </DropdownMenuItem>
                                )}
                            </>
                        )}
                        {canRemove && (
                            <>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem
                                    onClick={() => onRemove(member.id)}
                                    className="text-destructive"
                                >
                                    <TrashIcon className="h-4 w-4 mr-2" />
                                    Remove Member
                                </DropdownMenuItem>
                            </>
                        )}
                    </DropdownMenuContent>
                </DropdownMenu>
            )}
        </div>
    );
}

// Real data component using API
export function MemberList({ bandId }: { bandId: string }) {
    const { data, isLoading, error } = useGetMembers(bandId);
    const { email } = useAuthContext();
    const updateRole = useUpdateMemberRole();
    const removeMember = useRemoveMember();
    const queryClient = useQueryClient();

    if (isLoading) {
        return (
            <div className="space-y-3">
                {Array.from({ length: 3 }).map((_, i) => (
                    <div
                        key={i}
                        className="flex items-center justify-between p-4 border rounded-lg"
                    >
                        <div className="flex items-center gap-3">
                            <Skeleton className="h-10 w-10 rounded-full" />
                            <div className="space-y-2">
                                <Skeleton className="h-4 w-32" />
                                <Skeleton className="h-3 w-24" />
                            </div>
                        </div>
                        <Skeleton className="h-8 w-8" />
                    </div>
                ))}
            </div>
        );
    }

    if (error || !data) {
        return (
            <div className="p-4 border rounded-lg text-center text-muted-foreground">
                Failed to load members or no permission to view them.
            </div>
        );
    }

    const members = data?.value || [];
    const canManage = members.some(
        member => member.email === email && member.role === "Owner",
    );

    const handleRoleChange = (
        memberId: string,
        newRole: "Admin" | "Member",
    ) => {
        void updateRole.mutateAsync({
            bandId,
            memberId,
            data: { newRole },
        }).then(() => queryClient.invalidateQueries({
            queryKey: getGetMembersQueryKey(bandId),
        }));
    };

    const handleRemove = (memberId: string) => {
        void removeMember.mutateAsync({ bandId, memberId }).then(() =>
            queryClient.invalidateQueries({
                queryKey: getGetMembersQueryKey(bandId),
            }),
        );
    };

    if (members.length === 0) {
        return (
            <div className="p-4 border rounded-lg text-center text-muted-foreground">
                No members found.
            </div>
        );
    }

    return (
        <div className="space-y-3">
            {members.map((member) => (
                <MemberCard
                    key={member.id}
                    member={member}
                    onRoleChange={handleRoleChange}
                    onRemove={handleRemove}
                    canManage={canManage}
                />
            ))}
        </div>
    );
}
