import { ClockIcon, CheckIcon, XIcon, MailIcon } from "lucide-react";
// import { useGetMyInvitations } from "@/lib/generated-api/user-invitations";
// import { useRespondInvitation } from "@/lib/generated-api/user-invitations";
import { Button } from "@/shared/ui/button";
import { Badge } from "@/shared/ui/badge";
import { Card, CardContent } from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";
import {
    getGetMyInvitationsCountQueryKey,
    getGetMyInvitationsQueryKey,
    useGetMyInvitations,
    useRespondInvitation,
} from "@/lib/generated-api/band-api/user";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthContext } from "@/providers/auth/AuthorizationProviderContext";
import {
    ApiCodes,
    type GetMyInvitationsResponse,
} from "@/lib/generated-api/band-api/models";

interface InvitationCardProps {
    invitation: GetMyInvitationsResponse;
    onAccept: (invitationId: string) => void;
    onDecline: (invitationId: string) => void;
}

export function InvitationCard({
    invitation,
    onAccept,
    onDecline,
}: InvitationCardProps) {
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
                            <span className="font-medium">Band Invitation</span>
                            <Badge variant="outline" className="text-xs">
                                {invitation.role}
                            </Badge>
                        </div>
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <span>Invited by {invitation.inviterEmail}</span>
                        </div>
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <span>Band: {invitation.bandName}</span>
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
                            {invitation.status}
                        </Badge>
                        {invitation.status === "Pending" && (
                            <div className="flex gap-1">
                                <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={() => onAccept(invitation.id)}
                                    className="h-8 px-2"
                                >
                                    <CheckIcon className="h-3 w-3" />
                                </Button>
                                <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={() => onDecline(invitation.id)}
                                    className="h-8 px-2 text-destructive hover:text-destructive"
                                >
                                    <XIcon className="h-3 w-3" />
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
                    <p className="text-muted-foreground text-center">
                        Failed to load invitations or no permission to view
                        them.
                    </p>
                </CardContent>
            </Card>
        );
    }

    const invitations = data?.value || [];

    const handleAccept = async (invitationId: string) => {
        console.log("Accept invitation:", invitationId);

        // Find the invitation to check permissions
        const invitation = invitations.find(
            (inv: any) => inv.id === invitationId,
        );
        if (!invitation) {
            console.error("Invitation not found");
            return;
        }

        // Check if current user is the invitee
        if (invitation.inviteeEmail !== currentUserEmail) {
            console.error(
                "Permission denied: Only the invitee can respond to this invitation",
            );
            return;
        }

        try {
            await respondInvitation.mutateAsync({
                invitationId,
                data: { accept: true },
            });
            queryClient.invalidateQueries({
                queryKey: getGetMyInvitationsQueryKey(),
            });
            queryClient.invalidateQueries({
                queryKey: getGetMyInvitationsCountQueryKey(),
            });
        } catch (error) {
            console.error("Failed to accept invitation:", error);
        }
    };

    const handleDecline = async (invitationId: string) => {
        console.log("Decline invitation:", invitationId);

        // Find the invitation to check permissions
        const invitation = invitations.find(
            (inv: any) => inv.id === invitationId,
        );
        if (!invitation) {
            console.error("Invitation not found");
            return;
        }

        // Check if current user is the invitee
        if (invitation.inviteeEmail !== currentUserEmail) {
            console.error(
                "Permission denied: Only the invitee can respond to this invitation",
            );
            return;
        }

        try {
            await respondInvitation.mutateAsync({
                invitationId,
                data: { accept: false },
            });
            queryClient.invalidateQueries({
                queryKey: getGetMyInvitationsQueryKey(),
            });
            queryClient.invalidateQueries({
                queryKey: getGetMyInvitationsCountQueryKey(),
            });
        } catch (error) {
            console.error("Failed to decline invitation:", error);
        }
    };

    const pendingInvitations = invitations.filter(
        (inv: any) => inv.status === "Pending",
    );
    const otherInvitations = invitations.filter(
        (inv: any) => inv.status !== "Pending",
    );

    return (
        <div className="space-y-4">
            {pendingInvitations.length > 0 && (
                <div className="space-y-3">
                    <h4 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
                        Pending Invitations
                    </h4>
                    {pendingInvitations.map((invitation: any) => (
                        <InvitationCard
                            key={invitation.id}
                            invitation={invitation}
                            onAccept={handleAccept}
                            onDecline={handleDecline}
                        />
                    ))}
                </div>
            )}

            {otherInvitations.length > 0 && (
                <div className="space-y-3">
                    <h4 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
                        Recent Invitations
                    </h4>
                    {otherInvitations.map((invitation: any) => (
                        <InvitationCard
                            key={invitation.id}
                            invitation={invitation}
                            onAccept={handleAccept}
                            onDecline={handleDecline}
                        />
                    ))}
                </div>
            )}

            {invitations.length === 0 && (
                <Card>
                    <CardContent className="pt-6">
                        <p className="text-muted-foreground text-center">
                            No invitations found.
                        </p>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
