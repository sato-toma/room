let spinStopOverlappingPlayerIds = new Set<string>();

$.onUpdate(() => {
  const currentPlayerIds = new Set<string>();

  for (const overlap of $.getOverlaps()) {
    const player = overlap.handle;
    if (player?.type !== "player" || currentPlayerIds.has(player.id)) {
      continue;
    }

    currentPlayerIds.add(player.id);
    if (!spinStopOverlappingPlayerIds.has(player.id)) {
      player.send("spin-stop", null);
    }
  }

  spinStopOverlappingPlayerIds = currentPlayerIds;
});
