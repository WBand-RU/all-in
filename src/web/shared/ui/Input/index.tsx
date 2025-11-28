import { type ComponentProps } from "react";

interface InputProps extends ComponentProps<"input"> {
  error?: boolean;
}

export const Input = ({ error, className = '', ...props }: InputProps) => {
    const baseClasses = "flex h-12 w-full rounded-md border bg-input px-3 py-2 text-base ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50";
    
    const errorClasses = error 
        ? "border-red-500 focus-visible:ring-red-500" 
        : "border-input focus-visible:ring-blue-500";
    
    const classes = `${baseClasses} ${errorClasses} ${className}`;

    return (
        <input
            className={classes}
            {...props}
        />
    );
};
