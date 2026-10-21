"use client";

import { toast as notify } from "sonner";

type Notification = {
    title?: string;
    description?: string;
    variant?: "default" | "destructive";
};
function toast({ title = "", description, variant }: Notification) {
    return variant === "destructive"
        ? notify.error(title, { description })
        : notify.success(title, { description });
}

export function useToast() {
    return { toast };
}
