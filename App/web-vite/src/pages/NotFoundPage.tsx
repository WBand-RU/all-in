import { Link } from "react-router";
import { Button } from "../shared/ui/button";

export function NotFoundPage() {
	return (
		<div className="flex h-full items-center justify-center">
			<div className="flex flex-col items-center gap-4">
				<h1 className="text-4xl text-red-500">
					Page is not found!
				</h1>

				<p>Please, go to the home page</p>

				<Button asChild>
					<Link to="/">Go To Home</Link>
				</Button>
			</div>
		</div>
	);
}
