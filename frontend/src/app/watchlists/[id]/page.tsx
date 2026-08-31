import { notFound } from "next/navigation";
import { WatchlistDetailClient } from "@/components/watchlists/WatchlistDetailClient";

export default async function WatchlistDetailPage(props: PageProps<"/watchlists/[id]">) {
  const { id } = await props.params;
  const watchlistId = Number(id);

  if (!Number.isInteger(watchlistId) || watchlistId <= 0) {
    notFound();
  }

  return <WatchlistDetailClient watchlistId={watchlistId} />;
}
