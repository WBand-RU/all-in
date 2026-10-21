"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";

export function NotFoundPage() {
    return (
        <div className="flex h-full items-center justify-center">
            <div className="flex flex-col items-center gap-4">
                <h1 className="text-4xl text-red-500">Page is not found!</h1>

                <p>Please, go to the home page</p>

                <Button render={<Link href="/" />}>Go To Home</Button>
            </div>
        </div>
    );
}
