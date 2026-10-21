"use client";

import { Languages } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";

export function LanguageSwitcher() {
    const { i18n, t } = useTranslation();
    const language = i18n.resolvedLanguage === "en" ? "en" : "ru";
    const nextLanguage = language === "ru" ? "en" : "ru";

    const changeLanguage = () => {
        document.cookie = `wband-language=${nextLanguage}; Path=/; Max-Age=31536000; SameSite=Lax`;
        void i18n.changeLanguage(nextLanguage);
    };

    return (
        <Button
            variant="ghost"
            size="sm"
            onClick={changeLanguage}
            title={t("language.label")}
        >
            <Languages className="mr-2 h-4 w-4" />
            {language.toUpperCase()}
        </Button>
    );
}
