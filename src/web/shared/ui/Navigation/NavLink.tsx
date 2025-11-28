import { Link, useLocation } from "react-router";
import { Button } from "shared/ui/Button";
import { type ComponentProps, type ReactNode } from "react";

interface NavLinkProps extends ComponentProps<typeof Link> {
    children: ReactNode;
}

export const NavLink = ({ to, children, ...props }: NavLinkProps) => {
    const location = useLocation();
    const isActive = location.pathname === to;

    return (
        <Link to={to} {...props}>
            <Button variant={isActive ? "default" : "link"}>{children}</Button>
        </Link>
    );
};