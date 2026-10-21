"use client";

import { ClockIcon, CheckIcon, XIcon, MailIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
    getGetMyInvitationsCountQueryKey,
    getGetListOfBandsQueryKey,
    getGetListOfSongsQueryKey,
    getGetMyInvitationsQueryKey,
    useGetMyInvitations,
    useRespondInvitation,
} from "@/lib/generated/web-api/client";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import {
    ApiCodes,
    type MyInvitationResponse,
} from "@/lib/generated/web-api/models";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

interface InvitationCardProps {
    invitation: MyInvitationResponse;
    busy?: boolean;
    onAccept: (invitationId: string) => void;
    onDecline: (invitationId: string) => void;
}

export function InvitationCard({
    invitation,
    busy,
    onAccept,
    onDecline,
}: InvitationCardProps) {
    const { t } = useTranslation();
    const getStatusIcon = (status: string) => {
        switch (status) {
            case "Pending":
                return <ClockIcon className="h-4 w-4" />;
            case "Accepted":
                return <CheckIcon className="h-4 w-4" />;
            case "Declined":
                return <XIcon className="h-4 w-4" />;
            default:
                return <MailIcon className="h-4 w-4" />;
        }
    };

    return (
        <Card>
            <CardContent className="pt-6">
                <div className="flex items-start justify-between">
                    <div className="space-y-2">
                        <div className="flex items-center gap-2">
                            <MailIcon className="h-4 w-4 text-muted-foreground" />
                            <span className="font-medium">
                                {t("invitations.bandInvitation")}
                            </span>
                            <Badge variant="outline" className="text-xs">
                                {t(`band.roles.${invitation.role}`)}
                            </Badge>
                        </div>
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <span>
                                {t("invitations.invitedBy", {
                                    email: invitation.inviterEmail,
                                })}
                            </span>
                        </div>
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <span>
                                {t("invitations.band", {
                                    name: invitation.bandName,
                                })}
                            </span>
                        </div>
                    </div>
                    <div className="flex items-center gap-2">
                        <Badge
                            variant={
                                invitation.status === "Pending"
                                    ? "secondary"
                                    : "outline"
                            }
                            className="flex items-center gap-1"
                        >
                            {getStatusIcon(invitation.status)}
                            {t(
                                `invitations.${invitation.status.toLowerCase()}`
                            )}
                        </Badge>
                        {invitation.status === "Pending" && (
                            <div className="flex gap-1">
                                <Button
                                    size="sm"
                                    variant="outline"
                                    disabled={busy}
                                    onClick={() => onAccept(invitation.id)}
                                    className="h-8 px-2"
                                >
                                    <CheckIcon className="h-3 w-3" />
                                    <span className="sr-only">
                                        {t("invitations.accept")}
                                    </span>
                                </Button>
                                <Button
                                    size="sm"
                                    variant="outline"
                                    disabled={busy}
                                    onClick={() => onDecline(invitation.id)}
                                    className="h-8 px-2 text-destructive hover:text-destructive"
                                >
                                    <XIcon className="h-3 w-3" />
                                    <span className="sr-only">
                                        {t("invitations.decline")}
                                    </span>
                                </Button>
                            </div>
                        )}
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

export function MyInvitations() {
    const { t } = useTranslation();
    const { data, isLoading, error } = useGetMyInvitations();
    const respondInvitation = useRespondInvitation();
    const queryClient = useQueryClient();
    const { email: currentUserEmail } = useAuthContext();

    if (isLoading) {
        return (
            <div className="space-y-3">
                {Array.from({ length: 3 }).map((_, i) => (
                    <Card key={i}>
                        <CardContent className="pt-6">
                            <div className="flex items-start justify-between">
                                <div className="space-y-2">
                                    <Skeleton className="h-4 w-32" />
                                    <Skeleton className="h-3 w-24" />
                                </div>
                                <div className="flex items-center gap-2">
                                    <Skeleton className="h-6 w-16" />
                                    <div className="flex gap-1">
                                        <Skeleton className="h-8 w-8" />
                                        <Skeleton className="h-8 w-8" />
                                    </div>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                ))}
            </div>
        );
    }

    if (error || !data || data.code !== ApiCodes.Success) {
        return (
            <Card>
                <CardContent className="pt-6">
                    <p className="text-center text-muted-foreground">
                        {t("invitations.loadFailed")}
                    </p>
                </CardContent>
            </Card>
        );
    }

    const invitations = data?.value || [];

    const respond = async (invitationId: string, accept: boolean) => {
        const invitation = invitations.find((item) => item.id === invitationId);
        if (
            !invitation ||
            invitation.inviteeEmail.toLowerCase() !==
                currentUserEmail?.toLowerCase()
        )
            return;
        try {
            await respondInvitation.mutateAsync({
                invitationId,
                data: { accept },
            });
            await Promise.all([
                queryClient.invalidateQueries({
                    queryKey: getGetMyInvitationsQueryKey(),
                }),
                queryClient.invalidateQueries({
                    queryKey: getGetMyInvitationsCountQueryKey(),
                }),
                queryClient.invalidateQueries({
                    queryKey: getGetListOfBandsQueryKey(),
                }),
                queryClient.invalidateQueries({
                    queryKey: getGetListOfSongsQueryKey(),
                }),
                queryClient.invalidateQueries({ queryKey: ["my-band-access"] }),
            ]);
        } catch {
            toast.error(t("errors.generic"));
        }
    };
    const handleAccept = (id: string) => {
        void respond(id, true);
    };
    const handleDecline = (id: string) => {
        void respond(id, false);
    };

    const pendingInvitations = invitations.filter(
        (inv) => inv.status === "Pending"
    );
    const otherInvitations = invitations.filter(
        (inv) => inv.status !== "Pending"
    );

    return (
        <div className="space-y-4">
            {pendingInvitations.length > 0 && (
                <div className="space-y-3">
                    <h4 className="text-sm font-medium tracking-wide text-muted-foreground uppercase">
                        {t("invitations.pendingTitle")}
                    </h4>
                    {pendingInvitations.map((invitation) => (
                        <InvitationCard
                            key={invitation.id}
                            invitation={invitation}
                            busy={respondInvitation.isPending}
                            onAccept={handleAccept}
                            onDecline={handleDecline}
                        />
                    ))}
                </div>
            )}

            {otherInvitations.length > 0 && (
                <div className="space-y-3">
                    <h4 className="text-sm font-medium tracking-wide text-muted-foreground uppercase">
                        {t("invitations.recentTitle")}
                    </h4>
                    {otherInvitations.map((invitation) => (
                        <InvitationCard
                            key={invitation.id}
                            invitation={invitation}
                            busy={respondInvitation.isPending}
                            onAccept={handleAccept}
                            onDecline={handleDecline}
                        />
                    ))}
                </div>
            )}

            {invitations.length === 0 && (
                <Card>
                    <CardContent className="pt-6">
                        <p className="text-center text-muted-foreground">
                            {t("invitations.empty")}
                        </p>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
