"use client";

import {
    MoreVerticalIcon,
    ShieldIcon,
    UserIcon,
    TrashIcon,
    LogOutIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Skeleton } from "@/components/ui/skeleton";
import {
    getGetMembersQueryKey,
    useGetMembers,
    useRemoveMember,
    useUpdateMemberRole,
} from "@/lib/generated/web-api/client";
import type { BandMember } from "@/lib/generated/web-api/models";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import { useQueryClient } from "@tanstack/react-query";
import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/axios-instance";
import { getGetListOfBandsQueryKey } from "@/lib/generated/web-api/client";
import { useNavigator } from "@/services/navigator";
import { useToast } from "@/hooks/use-toast";
import { useTranslation } from "react-i18next";

interface MemberCardProps {
    member: BandMember;
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
    const { id, role } = member;
    if (!id || !role) return null;
    const canRemove = canManage && role !== "Owner";

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
        <div className="flex items-center justify-between rounded-lg border p-4">
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
                            variant={getRoleBadgeVariant(role)}
                            className="text-xs"
                        >
                            {getRoleIcon(role)}
                            <span className="ml-1">
                                {t(`band.roles.${role}`)}
                            </span>
                        </Badge>
                        <span className="text-xs text-muted-foreground">
                            {t("band.joined", {
                                date: member.joinedAt
                                    ? new Date(
                                          member.joinedAt
                                      ).toLocaleDateString()
                                    : "—",
                            })}
                        </span>
                    </div>
                </div>
            </div>

            {canManage && (
                <DropdownMenu>
                    <DropdownMenuTrigger
                        render={<Button variant="ghost" size="sm" />}
                    >
                        <MoreVerticalIcon className="h-4 w-4" />
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                        {role !== "Owner" && (
                            <>
                                {role !== "Admin" && (
                                    <DropdownMenuItem
                                        onClick={() =>
                                            onRoleChange(id, "Admin")
                                        }
                                    >
                                        <ShieldIcon className="mr-2 h-4 w-4" />
                                        {t("band.makeAdmin")}
                                    </DropdownMenuItem>
                                )}
                                {role === "Admin" && (
                                    <DropdownMenuItem
                                        onClick={() =>
                                            onRoleChange(id, "Member")
                                        }
                                    >
                                        <UserIcon className="mr-2 h-4 w-4" />
                                        {t("band.makeMember")}
                                    </DropdownMenuItem>
                                )}
                            </>
                        )}
                        {canRemove && (
                            <>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem
                                    onClick={() => onRemove(id)}
                                    className="text-destructive"
                                >
                                    <TrashIcon className="mr-2 h-4 w-4" />
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
        mutationFn: () =>
            apiClient.delete(`/band-api/bands/${bandId}/membership`),
        onSuccess: async () => {
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: getGetListOfBandsQueryKey(),
                }),
                queryClient.invalidateQueries({ queryKey: ["my-band-access"] }),
                queryClient.invalidateQueries({ queryKey: ["former-bands"] }),
                queryClient.invalidateQueries({
                    queryKey: ["/song-api/songs"],
                }),
            ]);
            toast({ title: t("band.left") });
            go("/bands");
        },
        onError: () =>
            toast({
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
                        className="flex items-center justify-between rounded-lg border p-4"
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
            <div className="rounded-lg border p-4 text-center text-muted-foreground">
                {t("band.loadMembersFailed")}
            </div>
        );
    }

    const members = data?.value || [];
    const canManage = members.some(
        (member) =>
            member.email.toLowerCase() === email?.toLowerCase() &&
            member.role === "Owner"
    );
    const currentMember = members.find(
        (member) => member.email.toLowerCase() === email?.toLowerCase()
    );

    const handleRoleChange = (
        memberId: string,
        newRole: "Admin" | "Member"
    ) => {
        void updateRole
            .mutateAsync({
                bandId,
                memberId,
                data: { newRole },
            })
            .then(async () => {
                await queryClient.invalidateQueries({
                    queryKey: getGetMembersQueryKey(bandId),
                });
                toast({
                    title: t(
                        newRole === "Admin"
                            ? "band.roleAdminSet"
                            : "band.roleMemberSet"
                    ),
                });
            })
            .catch(() =>
                toast({
                    title: t("band.roleChangeFailed"),
                    variant: "destructive",
                })
            );
    };

    const handleRemove = (memberId: string) => {
        void removeMember.mutateAsync({ bandId, memberId }).then(() =>
            queryClient.invalidateQueries({
                queryKey: getGetMembersQueryKey(bandId),
            })
        );
    };

    if (members.length === 0) {
        return (
            <div className="rounded-lg border p-4 text-center text-muted-foreground">
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
