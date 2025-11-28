import type {
    HTMLInputAutoCompleteAttribute,
    HTMLInputTypeAttribute,
} from "react";
import { Input } from "./Input";

type Props = {
    name: string;
    title: string;
    type?: HTMLInputTypeAttribute;
    autoComplete?: HTMLInputAutoCompleteAttribute;
    required?: boolean;
};

export function FormControl(props: Props) {
    return (
        <div>
            <label
                htmlFor={props.name}
                className="block text-base leading-6 font-semibold text-foreground mb-1"
            >
                {props.title}
            </label>
            <div className="mt-2">
                <Input
                    id={props.name}
                    name={props.name}
                    type={props.type}
                    autoComplete={props.autoComplete}
                    required={props.required}
                />
            </div>
        </div>
    );
}
