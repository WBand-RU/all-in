import { useState, type ChangeEvent, type KeyboardEvent } from "react";
import { useTranslation } from "react-i18next";
import { Input } from "./input";

interface Props {
    defaultValue: string;
    onEdited: (newValue: string) => void;
}

export function EditableText({ defaultValue, onEdited }: Props) {
    const [value, setValue] = useState(defaultValue);
    const [isEdit, setIsEdit] = useState(false);
    const { t } = useTranslation("translation");

    function edit() {
        if (isEdit) {
            throw new Error("Invalid state: isEdit is set");
        }

        setIsEdit(true);
    }

    function handleInputChange(ev: ChangeEvent<HTMLInputElement>) {
        setValue(ev.target.value);
    }

    function handleInputKeyDown(ev: KeyboardEvent<HTMLInputElement>) {
        console.log("code:", ev);

        if (ev.key != "Enter") {
            return;
        }

        onEdited(value);
        setIsEdit(false);
    }

    if (!isEdit) {
        return (
            <span className="cursor-pointer"
                title={t("you_can_edit_name")}
                onClick={edit}>
                {value}
            </span>
        );
    }

    return (
        <Input
            value={value}
            onChange={handleInputChange}
            onKeyDown={handleInputKeyDown}
            placeholder="Enter new name"
            type="text" />
    );
}