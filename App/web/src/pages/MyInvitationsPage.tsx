import { MyInvitations } from "@/widgets/bands/ui/MyInvitations";

export function MyInvitationsPage() {
    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-bold">My Invitations</h1>
                <p className="text-muted-foreground">
                    Manage your pending band invitations.
                </p>
            </div>

            <MyInvitations />
        </div>
    );
}
