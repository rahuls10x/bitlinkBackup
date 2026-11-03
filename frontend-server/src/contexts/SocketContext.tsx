import React, { createContext, useContext, useEffect, useState, useCallback, useRef } from "react";
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useAuth } from "./AuthContext";

export interface ISocketClient {
	emit: <T = unknown>(event: string, data?: unknown, callback?: (response: T) => void) => void;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	on: (event: string, handler: (...args: any[]) => void) => void;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	off: (event: string, handler: (...args: any[]) => void) => void;
}

interface SocketContextType {
	socket: ISocketClient | null;
	isConnected: boolean;
	disconnect: () => void;
}

const SocketContext = createContext<SocketContextType>({
	socket: null,
	isConnected: false,
	disconnect: () => {},
});

export function SocketProvider({ children }: { children: React.ReactNode }) {
	const { isAuthenticated, registerBeforeLogout } = useAuth();
	const [socket, setSocket] = useState<ISocketClient | null>(null);
	const [isConnected, setIsConnected] = useState(false);
	const connectionRef = useRef<HubConnection | null>(null);

	const disconnect = useCallback(() => {
		if (connectionRef.current) {
			console.log("[SignalR] Disconnecting SignalR connection");
			connectionRef.current.stop();
			connectionRef.current = null;
			setSocket(null);
			setIsConnected(false);
		}
	}, []);

	// Guarantee WebSocket disconnects prior to backend session revocation
	useEffect(() => {
		const unregister = registerBeforeLogout?.(() => {
			disconnect();
		});
		return unregister;
	}, [registerBeforeLogout, disconnect]);

	useEffect(() => {
		if (!isAuthenticated) {
			disconnect();
			return;
		}

		const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:4001";
		const hubUrl = `${apiUrl}/hubs/files`;

		const connection = new HubConnectionBuilder()
			.withUrl(hubUrl, {
				withCredentials: true,
			})
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();

		connectionRef.current = connection;

		// Create compatible client adapter implementing emit, on, off
		const clientAdapter: ISocketClient = {
			emit: <T = unknown>(event: string, data?: unknown, callback?: (response: T) => void) => {
				if (connection.state === HubConnectionState.Connected) {
					// In SignalR, methodName is case-insensitive
					const promise = data !== undefined ? connection.invoke(event, data) : connection.invoke(event);
					promise
						.then((res: unknown) => {
							callback?.(res as T);
						})
						.catch((err: unknown) => {
							const errorMessage = err instanceof Error ? err.message : "SignalR invocation error";
							callback?.({ success: false, error: errorMessage } as T);
						});
				} else {
					callback?.({ success: false, error: "Connection is not open" } as T);
				}
			},
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			on: (event: string, handler: (...args: any[]) => void) => {
				connection.on(event, handler);
			},
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			off: (event: string, handler: (...args: any[]) => void) => {
				connection.off(event, handler);
			},
		};

		connection.onclose((error) => {
			setIsConnected(false);
			console.log("[SignalR] Disconnected from server:", error);
		});

		connection.onreconnecting((error) => {
			setIsConnected(false);
			console.log("[SignalR] Reconnecting:", error);
		});

		connection.onreconnected(() => {
			setIsConnected(true);
			console.log("[SignalR] Reconnected to server");
		});

		connection
			.start()
			.then(() => {
				setIsConnected(true);
				setSocket(clientAdapter);
				console.log("[SignalR] Connected to server hub:", hubUrl);
			})
			.catch((err) => {
				console.error("[SignalR] Error connecting to hub:", err);
				setIsConnected(false);
			});

		return () => {
			if (connectionRef.current === connection) {
				connection.stop();
				connectionRef.current = null;
			}
		};
	}, [isAuthenticated, disconnect]);

	return (
		<SocketContext.Provider value={{ socket, isConnected, disconnect }}>
			{children}
		</SocketContext.Provider>
	);
}

export const useSocket = () => useContext(SocketContext);

