import type { LucideIcon } from "lucide-react";

export type MenuItem = {
    title: string;
    url: string;
    icon?: LucideIcon;
    badge?: number;
};
