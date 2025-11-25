import { toast } from "@/shared/ui/use-toast";

interface OfflineData {
    songs: any[];
    playlists: any[];
    playbacks: any[];
    messages: any[];
    lastSyncTime: Date;
}

interface SyncConflict {
    id: string;
    type: string;
    localData: any;
    remoteData: any;
    resolution?: "local" | "remote" | "merge";
}

class OfflineService {
    private readonly DB_NAME = "WBandOfflineDB";
    private readonly DB_VERSION = 1;
    private db: IDBDatabase | null = null;
    private isOnline: boolean = navigator.onLine;
    private pendingOperations: any[] = [];

    constructor() {
        this.initializeDB();
        this.setupEventListeners();
    }

    private async initializeDB() {
        return new Promise<void>((resolve, reject) => {
            const request = indexedDB.open(this.DB_NAME, this.DB_VERSION);

            request.onerror = () => {
                console.error("Failed to open IndexedDB");
                reject(request.error);
            };

            request.onsuccess = () => {
                this.db = request.result;
                console.log("IndexedDB initialized");
                resolve();
            };

            request.onupgradeneeded = (event: any) => {
                const db = event.target.result;

                // Create object stores for different data types
                if (!db.objectStoreNames.contains("songs")) {
                    const songsStore = db.createObjectStore("songs", {
                        keyPath: "id",
                    });
                    songsStore.createIndex("bandId", "bandId", {
                        unique: false,
                    });
                    songsStore.createIndex("syncStatus", "syncStatus", {
                        unique: false,
                    });
                }

                if (!db.objectStoreNames.contains("playlists")) {
                    const playlistsStore = db.createObjectStore("playlists", {
                        keyPath: "id",
                    });
                    playlistsStore.createIndex("bandId", "bandId", {
                        unique: false,
                    });
                    playlistsStore.createIndex("syncStatus", "syncStatus", {
                        unique: false,
                    });
                }

                if (!db.objectStoreNames.contains("playbacks")) {
                    const playbacksStore = db.createObjectStore("playbacks", {
                        keyPath: "id",
                    });
                    playbacksStore.createIndex("songId", "songId", {
                        unique: false,
                    });
                    playbacksStore.createIndex("syncStatus", "syncStatus", {
                        unique: false,
                    });
                }

                if (!db.objectStoreNames.contains("messages")) {
                    const messagesStore = db.createObjectStore("messages", {
                        keyPath: "id",
                    });
                    messagesStore.createIndex("bandId", "bandId", {
                        unique: false,
                    });
                    messagesStore.createIndex("timestamp", "timestamp", {
                        unique: false,
                    });
                }

                if (!db.objectStoreNames.contains("pendingOperations")) {
                    const pendingStore = db.createObjectStore(
                        "pendingOperations",
                        {
                            keyPath: "id",
                            autoIncrement: true,
                        },
                    );
                    pendingStore.createIndex("timestamp", "timestamp", {
                        unique: false,
                    });
                }

                if (!db.objectStoreNames.contains("syncMetadata")) {
                    db.createObjectStore("syncMetadata", { keyPath: "key" });
                }
            };
        });
    }

    private setupEventListeners() {
        window.addEventListener("online", () => {
            this.isOnline = true;
            console.log("Back online - starting sync");
            this.syncWithServer();
        });

        window.addEventListener("offline", () => {
            this.isOnline = false;
            console.log("Gone offline - enabling offline mode");
            toast({
                title: "Offline Mode",
                description:
                    "You're working offline. Changes will be synced when connection is restored.",
            });
        });
    }

    // Save data locally
    async saveLocally(storeName: string, data: any) {
        if (!this.db) await this.initializeDB();

        return new Promise<void>((resolve, reject) => {
            const transaction = this.db!.transaction([storeName], "readwrite");
            const store = transaction.objectStore(storeName);

            // Add sync metadata
            const dataWithMetadata = {
                ...data,
                syncStatus: "pending",
                localTimestamp: new Date().toISOString(),
                isOfflineChange: true,
            };

            const request = store.put(dataWithMetadata);

            request.onsuccess = () => {
                console.log(`Data saved locally to ${storeName}`);
                resolve();
            };

            request.onerror = () => {
                console.error(`Failed to save data to ${storeName}`);
                reject(request.error);
            };
        });
    }

    // Get data from local storage
    async getLocalData(storeName: string, id?: string): Promise<any> {
        if (!this.db) await this.initializeDB();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction([storeName], "readonly");
            const store = transaction.objectStore(storeName);

            const request = id ? store.get(id) : store.getAll();

            request.onsuccess = () => {
                resolve(request.result);
            };

            request.onerror = () => {
                reject(request.error);
            };
        });
    }

    // Queue an operation for later sync
    async queueOperation(operation: any) {
        if (!this.db) await this.initializeDB();

        const operationData = {
            ...operation,
            timestamp: new Date().toISOString(),
            status: "pending",
        };

        return new Promise<void>((resolve, reject) => {
            const transaction = this.db!.transaction(
                ["pendingOperations"],
                "readwrite",
            );
            const store = transaction.objectStore("pendingOperations");
            const request = store.add(operationData);

            request.onsuccess = () => {
                this.pendingOperations.push(operationData);
                console.log("Operation queued for sync");
                resolve();
            };

            request.onerror = () => {
                reject(request.error);
            };
        });
    }

    // Sync with server
    async syncWithServer() {
        if (!this.isOnline) {
            console.log("Cannot sync - offline");
            return;
        }

        try {
            toast({
                title: "Syncing",
                description: "Synchronizing your offline changes...",
            });

            // Get all pending operations
            const pendingOps = await this.getPendingOperations();

            // Process each operation
            for (const op of pendingOps) {
                try {
                    await this.processPendingOperation(op);
                    await this.markOperationComplete(op.id);
                } catch (error) {
                    console.error(`Failed to sync operation ${op.id}:`, error);
                }
            }

            // Pull latest data from server
            await this.pullServerData();

            // Update last sync time
            await this.updateSyncMetadata(
                "lastSyncTime",
                new Date().toISOString(),
            );

            toast({
                title: "Sync Complete",
                description: "All changes have been synchronized.",
            });
        } catch (error) {
            console.error("Sync failed:", error);
            toast({
                title: "Sync Failed",
                description:
                    "Some changes couldn't be synchronized. Will retry later.",
                variant: "destructive",
            });
        }
    }

    private async getPendingOperations(): Promise<any[]> {
        if (!this.db) await this.initializeDB();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(
                ["pendingOperations"],
                "readonly",
            );
            const store = transaction.objectStore("pendingOperations");
            const request = store.getAll();

            request.onsuccess = () => {
                resolve(request.result);
            };

            request.onerror = () => {
                reject(request.error);
            };
        });
    }

    private async processPendingOperation(operation: any) {
        // This would make actual API calls to sync the operation
        console.log("Processing operation:", operation);

        // Simulate API call
        return new Promise((resolve) => {
            setTimeout(() => {
                resolve(true);
            }, 500);
        });
    }

    private async markOperationComplete(operationId: number) {
        if (!this.db) await this.initializeDB();

        return new Promise<void>((resolve, reject) => {
            const transaction = this.db!.transaction(
                ["pendingOperations"],
                "readwrite",
            );
            const store = transaction.objectStore("pendingOperations");
            const request = store.delete(operationId);

            request.onsuccess = () => {
                resolve();
            };

            request.onerror = () => {
                reject(request.error);
            };
        });
    }

    private async pullServerData() {
        // This would fetch latest data from server
        console.log("Pulling latest data from server");

        // Simulate fetching data
        return new Promise((resolve) => {
            setTimeout(() => {
                resolve(true);
            }, 1000);
        });
    }

    private async updateSyncMetadata(key: string, value: any) {
        if (!this.db) await this.initializeDB();

        return new Promise<void>((resolve, reject) => {
            const transaction = this.db!.transaction(
                ["syncMetadata"],
                "readwrite",
            );
            const store = transaction.objectStore("syncMetadata");
            const request = store.put({ key, value });

            request.onsuccess = () => {
                resolve();
            };

            request.onerror = () => {
                reject(request.error);
            };
        });
    }

    // Check if offline mode is enabled
    isOfflineMode(): boolean {
        return !this.isOnline;
    }

    // Manual sync trigger
    async manualSync() {
        if (!this.isOnline) {
            toast({
                title: "No Connection",
                description: "Cannot sync while offline",
                variant: "destructive",
            });
            return;
        }

        await this.syncWithServer();
    }

    // Clear all offline data
    async clearOfflineData() {
        if (!this.db) await this.initializeDB();

        const stores = [
            "songs",
            "playlists",
            "playbacks",
            "messages",
            "pendingOperations",
            "syncMetadata",
        ];

        const transaction = this.db!.transaction(stores, "readwrite");

        for (const storeName of stores) {
            const store = transaction.objectStore(storeName);
            store.clear();
        }

        return new Promise<void>((resolve, reject) => {
            transaction.oncomplete = () => {
                console.log("Offline data cleared");
                toast({
                    title: "Data Cleared",
                    description: "All offline data has been removed",
                });
                resolve();
            };

            transaction.onerror = () => {
                reject(transaction.error);
            };
        });
    }

    // Export offline data
    async exportOfflineData(): Promise<OfflineData> {
        if (!this.db) await this.initializeDB();

        const songs = await this.getLocalData("songs");
        const playlists = await this.getLocalData("playlists");
        const playbacks = await this.getLocalData("playbacks");
        const messages = await this.getLocalData("messages");

        return {
            songs,
            playlists,
            playbacks,
            messages,
            lastSyncTime: new Date(),
        };
    }

    // Import offline data
    async importOfflineData(data: OfflineData) {
        if (!this.db) await this.initializeDB();

        try {
            // Import each data type
            for (const song of data.songs) {
                await this.saveLocally("songs", song);
            }

            for (const playlist of data.playlists) {
                await this.saveLocally("playlists", playlist);
            }

            for (const playback of data.playbacks) {
                await this.saveLocally("playbacks", playback);
            }

            for (const message of data.messages) {
                await this.saveLocally("messages", message);
            }

            toast({
                title: "Import Complete",
                description: "Offline data imported successfully",
            });
        } catch (error) {
            console.error("Import failed:", error);
            toast({
                title: "Import Failed",
                description: "Failed to import offline data",
                variant: "destructive",
            });
        }
    }
}

// Export singleton instance
export const offlineService = new OfflineService();
