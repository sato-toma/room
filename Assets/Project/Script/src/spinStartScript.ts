let spinStartOverlappingPlayerIds = new Set<string>();

$.onUpdate(() => {
  const currentPlayerIds = new Set<string>();

  for (const overlap of $.getOverlaps()) {
    const player = overlap.handle;
    if (player?.type !== "player" || currentPlayerIds.has(player.id)) {
      continue;
    }

    currentPlayerIds.add(player.id);
    if (!spinStartOverlappingPlayerIds.has(player.id)) {
      const direction = new Vector3(0, 0, 1).applyQuaternion($.getRotation());
      if (direction.lengthSq() > 0) {
        direction.normalize();
      }

      $.setPlayerScript(player);
      player.send("spin-direction", direction);
    }
  }

  spinStartOverlappingPlayerIds = currentPlayerIds;
});
