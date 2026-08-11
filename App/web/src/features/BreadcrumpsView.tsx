import { Link } from "react-router";
import {
	Breadcrumb,
	BreadcrumbItem,
	BreadcrumbLink,
	BreadcrumbList,
	BreadcrumbSeparator
} from "../shared/ui/breadcrumb";
import { Segments } from "../routes";

type Item = {
	segment: string;
	title: string;
	children?: Item[];
};

const items: Item[] = [
	{
		segment: Segments.app,
		title: "Dashboard",
	},
];


export function BreadcrumpsView() {


	return (
		<Breadcrumb>
			<BreadcrumbList>
				<BreadcrumbItem>
					<BreadcrumbLink asChild>
						<Link to="/app">Dashboard</Link>
					</BreadcrumbLink>
				</BreadcrumbItem>
				{items.map(x => (
					<>
						<BreadcrumbSeparator key={`separator-${x.segment}`} />

						<BreadcrumbItem key={`item-${x.segment}`}
							className={x === items[items.length - 1] ? "font-bold" : ""}>
							<BreadcrumbLink asChild>
								<Link to={x.segment}>{x.title}</Link>
							</BreadcrumbLink>
						</BreadcrumbItem>
					</>
				))}
			</BreadcrumbList>
		</Breadcrumb>
	);
}
