import { useState } from "react";
import { useNavigate } from "react-router";
import {
    Search,
    Music,
    ListMusic,
    Play,
    Users,
    MessageSquare,
    Filter,
    X,
} from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Badge } from "@/shared/ui/badge";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/shared/ui/tabs";
import { Checkbox } from "@/shared/ui/checkbox";
import { Label } from "@/shared/ui/label";

interface SearchResult {
    id: string;
    type: "song" | "playlist" | "playback" | "member" | "message";
    title: string;
    description?: string;
    metadata?: any;
    timestamp?: Date;
    highlighted?: string[];
}

export function SearchPage() {
    const navigate = useNavigate();
    const [searchQuery, setSearchQuery] = useState("");
    const [activeTab, setActiveTab] = useState("all");
    const [showFilters, setShowFilters] = useState(false);
    const [isSearching, setIsSearching] = useState(false);

    const [filters, setFilters] = useState({
        songs: true,
        playlists: true,
        playbacks: true,
        members: true,
        messages: true,
        dateRange: "all", // all, week, month, year
    });

    const [results] = useState<SearchResult[]>([
        {
            id: "1",
            type: "song",
            title: "Amazing Grace",
            description: "Traditional hymn in G major",
            metadata: { author: "John Newton", key: "G", bpm: 72 },
        },
        {
            id: "2",
            type: "playlist",
            title: "Sunday Morning Service",
            description: "8 songs, 45 minutes",
            metadata: { songCount: 8, duration: 45 },
        },
        {
            id: "3",
            type: "playback",
            title: "How Great Thou Art - Multitrack",
            description: "5 tracks, mixed version",
            metadata: { trackCount: 5 },
        },
        {
            id: "4",
            type: "member",
            title: "John Smith",
            description: "Lead Vocalist",
            metadata: { role: "Owner" },
        },
        {
            id: "5",
            type: "message",
            title: "Rehearsal schedule update",
            description: "Can we move rehearsal to 6 PM?",
            timestamp: new Date(Date.now() - 3600000),
            metadata: { sender: "Sarah Johnson" },
        },
    ]);

    const getFilteredResults = () => {
        let filtered = results;

        if (activeTab !== "all") {
            filtered = filtered.filter((r) => r.type === activeTab);
        }

        filtered = filtered.filter((r) => {
            if (!filters.songs && r.type === "song") return false;
            if (!filters.playlists && r.type === "playlist") return false;
            if (!filters.playbacks && r.type === "playback") return false;
            if (!filters.members && r.type === "member") return false;
            if (!filters.messages && r.type === "message") return false;
            return true;
        });

        if (searchQuery) {
            filtered = filtered.filter(
                (r) =>
                    r.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                    r.description
                        ?.toLowerCase()
                        .includes(searchQuery.toLowerCase()),
            );
        }

        return filtered;
    };

    const handleSearch = () => {
        setIsSearching(true);
        // Simulate search delay
        setTimeout(() => {
            setIsSearching(false);
        }, 500);
    };

    const navigateToResult = (result: SearchResult) => {
        switch (result.type) {
            case "song":
                navigate(`/songs/${result.id}/edit`);
                break;
            case "playlist":
                navigate(`/playlists/${result.id}/edit`);
                break;
            case "playback":
                navigate(`/playback/${result.id}`);
                break;
            case "member":
                navigate(`/band/members/${result.id}`);
                break;
            case "message":
                navigate("/chat");
                break;
        }
    };

    const getResultIcon = (type: string) => {
        switch (type) {
            case "song":
                return <Music className="h-4 w-4" />;
            case "playlist":
                return <ListMusic className="h-4 w-4" />;
            case "playback":
                return <Play className="h-4 w-4" />;
            case "member":
                return <Users className="h-4 w-4" />;
            case "message":
                return <MessageSquare className="h-4 w-4" />;
            default:
                return null;
        }
    };

    const getResultBadgeVariant = (type: string) => {
        switch (type) {
            case "song":
                return "default";
            case "playlist":
                return "secondary";
            case "playback":
                return "outline";
            default:
                return "secondary";
        }
    };

    const filteredResults = getFilteredResults();

    return (
        <div className="container mx-auto py-8">
            <Card>
                <CardHeader>
                    <CardTitle>Global Search</CardTitle>
                    <CardDescription>
                        Search across songs, playlists, playbacks, members, and
                        messages
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {/* Search Bar */}
                    <div className="flex gap-2 mb-6">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                            <Input
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                onKeyPress={(e) =>
                                    e.key === "Enter" && handleSearch()
                                }
                                placeholder="Search for anything..."
                                className="pl-10"
                            />
                        </div>
                        <Button onClick={handleSearch} disabled={isSearching}>
                            {isSearching ? "Searching..." : "Search"}
                        </Button>
                        <Button
                            variant="outline"
                            onClick={() => setShowFilters(!showFilters)}
                        >
                            <Filter className="h-4 w-4 mr-2" />
                            Filters
                        </Button>
                    </div>

                    {/* Filters Panel */}
                    {showFilters && (
                        <Card className="mb-6 p-4">
                            <div className="flex justify-between items-center mb-4">
                                <h3 className="font-semibold">
                                    Search Filters
                                </h3>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setShowFilters(false)}
                                >
                                    <X className="h-4 w-4" />
                                </Button>
                            </div>
                            <div className="grid grid-cols-3 gap-4">
                                <div className="space-y-2">
                                    <Label>Content Types</Label>
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="filter-songs"
                                                checked={filters.songs}
                                                onCheckedChange={(checked) =>
                                                    setFilters({
                                                        ...filters,
                                                        songs: !!checked,
                                                    })
                                                }
                                            />
                                            <Label htmlFor="filter-songs">
                                                Songs
                                            </Label>
                                        </div>
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="filter-playlists"
                                                checked={filters.playlists}
                                                onCheckedChange={(checked) =>
                                                    setFilters({
                                                        ...filters,
                                                        playlists: !!checked,
                                                    })
                                                }
                                            />
                                            <Label htmlFor="filter-playlists">
                                                Playlists
                                            </Label>
                                        </div>
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="filter-playbacks"
                                                checked={filters.playbacks}
                                                onCheckedChange={(checked) =>
                                                    setFilters({
                                                        ...filters,
                                                        playbacks: !!checked,
                                                    })
                                                }
                                            />
                                            <Label htmlFor="filter-playbacks">
                                                Playbacks
                                            </Label>
                                        </div>
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <Label>&nbsp;</Label>
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="filter-members"
                                                checked={filters.members}
                                                onCheckedChange={(checked) =>
                                                    setFilters({
                                                        ...filters,
                                                        members: !!checked,
                                                    })
                                                }
                                            />
                                            <Label htmlFor="filter-members">
                                                Members
                                            </Label>
                                        </div>
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="filter-messages"
                                                checked={filters.messages}
                                                onCheckedChange={(checked) =>
                                                    setFilters({
                                                        ...filters,
                                                        messages: !!checked,
                                                    })
                                                }
                                            />
                                            <Label htmlFor="filter-messages">
                                                Messages
                                            </Label>
                                        </div>
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <Label>Date Range</Label>
                                    <div className="space-y-2">
                                        <Button
                                            variant={
                                                filters.dateRange === "all"
                                                    ? "default"
                                                    : "outline"
                                            }
                                            size="sm"
                                            className="w-full"
                                            onClick={() =>
                                                setFilters({
                                                    ...filters,
                                                    dateRange: "all",
                                                })
                                            }
                                        >
                                            All Time
                                        </Button>
                                        <Button
                                            variant={
                                                filters.dateRange === "week"
                                                    ? "default"
                                                    : "outline"
                                            }
                                            size="sm"
                                            className="w-full"
                                            onClick={() =>
                                                setFilters({
                                                    ...filters,
                                                    dateRange: "week",
                                                })
                                            }
                                        >
                                            Past Week
                                        </Button>
                                        <Button
                                            variant={
                                                filters.dateRange === "month"
                                                    ? "default"
                                                    : "outline"
                                            }
                                            size="sm"
                                            className="w-full"
                                            onClick={() =>
                                                setFilters({
                                                    ...filters,
                                                    dateRange: "month",
                                                })
                                            }
                                        >
                                            Past Month
                                        </Button>
                                    </div>
                                </div>
                            </div>
                        </Card>
                    )}

                    {/* Results Tabs */}
                    <Tabs value={activeTab} onValueChange={setActiveTab}>
                        <TabsList className="mb-4">
                            <TabsTrigger value="all">
                                All ({filteredResults.length})
                            </TabsTrigger>
                            <TabsTrigger value="song">
                                Songs (
                                {
                                    filteredResults.filter(
                                        (r) => r.type === "song",
                                    ).length
                                }
                                )
                            </TabsTrigger>
                            <TabsTrigger value="playlist">
                                Playlists (
                                {
                                    filteredResults.filter(
                                        (r) => r.type === "playlist",
                                    ).length
                                }
                                )
                            </TabsTrigger>
                            <TabsTrigger value="playback">
                                Playbacks (
                                {
                                    filteredResults.filter(
                                        (r) => r.type === "playback",
                                    ).length
                                }
                                )
                            </TabsTrigger>
                            <TabsTrigger value="member">
                                Members (
                                {
                                    filteredResults.filter(
                                        (r) => r.type === "member",
                                    ).length
                                }
                                )
                            </TabsTrigger>
                            <TabsTrigger value="message">
                                Messages (
                                {
                                    filteredResults.filter(
                                        (r) => r.type === "message",
                                    ).length
                                }
                                )
                            </TabsTrigger>
                        </TabsList>

                        <TabsContent value={activeTab} className="space-y-4">
                            {filteredResults.length === 0 ? (
                                <div className="text-center py-12">
                                    <Search className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                                    <h3 className="text-lg font-semibold mb-2">
                                        No results found
                                    </h3>
                                    <p className="text-muted-foreground">
                                        Try adjusting your search query or
                                        filters
                                    </p>
                                </div>
                            ) : (
                                <div className="space-y-2">
                                    {filteredResults.map((result) => (
                                        <div
                                            key={`${result.type}-${result.id}`}
                                            className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent cursor-pointer"
                                            onClick={() =>
                                                navigateToResult(result)
                                            }
                                        >
                                            <div className="flex items-center gap-3">
                                                <div className="p-2 bg-secondary rounded-full">
                                                    {getResultIcon(result.type)}
                                                </div>
                                                <div>
                                                    <div className="flex items-center gap-2">
                                                        <span className="font-medium">
                                                            {result.title}
                                                        </span>
                                                        <Badge
                                                            variant={getResultBadgeVariant(
                                                                result.type,
                                                            )}
                                                        >
                                                            {result.type}
                                                        </Badge>
                                                    </div>
                                                    {result.description && (
                                                        <p className="text-sm text-muted-foreground">
                                                            {result.description}
                                                        </p>
                                                    )}
                                                    {result.timestamp && (
                                                        <p className="text-xs text-muted-foreground">
                                                            {new Date(
                                                                result.timestamp,
                                                            ).toLocaleString()}
                                                        </p>
                                                    )}
                                                </div>
                                            </div>

                                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                                {result.metadata && (
                                                    <>
                                                        {result.metadata
                                                            .author && (
                                                            <span>
                                                                by{" "}
                                                                {
                                                                    result
                                                                        .metadata
                                                                        .author
                                                                }
                                                            </span>
                                                        )}
                                                        {result.metadata
                                                            .key && (
                                                            <Badge variant="outline">
                                                                Key:{" "}
                                                                {
                                                                    result
                                                                        .metadata
                                                                        .key
                                                                }
                                                            </Badge>
                                                        )}
                                                        {result.metadata
                                                            .songCount && (
                                                            <span>
                                                                {
                                                                    result
                                                                        .metadata
                                                                        .songCount
                                                                }{" "}
                                                                songs
                                                            </span>
                                                        )}
                                                        {result.metadata
                                                            .trackCount && (
                                                            <span>
                                                                {
                                                                    result
                                                                        .metadata
                                                                        .trackCount
                                                                }{" "}
                                                                tracks
                                                            </span>
                                                        )}
                                                        {result.metadata
                                                            .role && (
                                                            <Badge>
                                                                {
                                                                    result
                                                                        .metadata
                                                                        .role
                                                                }
                                                            </Badge>
                                                        )}
                                                        {result.metadata
                                                            .sender && (
                                                            <span>
                                                                from{" "}
                                                                {
                                                                    result
                                                                        .metadata
                                                                        .sender
                                                                }
                                                            </span>
                                                        )}
                                                    </>
                                                )}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </TabsContent>
                    </Tabs>
                </CardContent>
            </Card>
        </div>
    );
}
