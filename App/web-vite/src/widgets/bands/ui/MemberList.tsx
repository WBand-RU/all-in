import {
    MoreVerticalIcon,
    ShieldIcon,
    UserIcon,
    TrashIcon,
    LogOutIcon,
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
import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/axios-instance";
import { getGetListOfBandsQueryKey } from "@/lib/generated-api/band-api/bands";
import { useNavigator } from "@/services/navigator";
import { useToast } from "@/shared/ui/use-toast";
import { useTranslation } from "react-i18next";

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
    const { t } = useTranslation();
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
                                {t(`band.roles.${member.role}`)}
                            </span>
                        </Badge>
                        <span className="text-xs text-muted-foreground">
                            {t("band.joined", { date: new Date(member.joinedAt).toLocaleDateString() })}
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
                                        {t("band.makeAdmin")}
                                    </DropdownMenuItem>
                                )}
                                {member.role === "Admin" && (
                                    <DropdownMenuItem
                                        onClick={() =>
                                            onRoleChange(member.id, "Member")
                                        }
                                    >
                                        <UserIcon className="h-4 w-4 mr-2" />
                                        {t("band.makeMember")}
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
                                    {t("band.removeMember")}
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
    const { go } = useNavigator();
    const { toast } = useToast();
    const { t } = useTranslation();
    const leaveBand = useMutation({
        mutationFn: () => apiClient.delete(`/band-api/bands/${bandId}/membership`),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: getGetListOfBandsQueryKey() });
            toast({ title: t("band.left") });
            go("/bands");
        },
        onError: () => toast({
            title: t("band.leaveFailed"),
            description: t("errors.owner_cannot_leave"),
            variant: "destructive",
        }),
    });

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
                {t("band.loadMembersFailed")}
            </div>
        );
    }

    const members = data?.value || [];
    const canManage = members.some(
        member => member.email.toLowerCase() === email?.toLowerCase() && member.role === "Owner",
    );
    const currentMember = members.find(
        member => member.email.toLowerCase() === email?.toLowerCase(),
    );

    const handleRoleChange = (
        memberId: string,
        newRole: "Admin" | "Member",
    ) => {
        void updateRole.mutateAsync({
            bandId,
            memberId,
            data: { newRole },
        }).then(async () => {
            await queryClient.invalidateQueries({ queryKey: getGetMembersQueryKey(bandId) });
            toast({ title: t(newRole === "Admin" ? "band.roleAdminSet" : "band.roleMemberSet") });
        }).catch(() => toast({ title: t("band.roleChangeFailed"), variant: "destructive" }));
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
                {t("band.noMembers")}
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
            {currentMember && currentMember.role !== "Owner" && (
                <div className="flex justify-end pt-3">
                    <Button
                        variant="destructive"
                        disabled={leaveBand.isPending}
                        onClick={() => {
                            if (window.confirm(t("band.leaveConfirm")))
                                leaveBand.mutate();
                        }}
                    >
                        <LogOutIcon className="mr-2 h-4 w-4" />
                        {t("band.leave")}
                    </Button>
                </div>
            )}
        </div>
    );
}
