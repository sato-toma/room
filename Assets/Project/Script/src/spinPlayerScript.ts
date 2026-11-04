const moveSpeed = 2.5;
const spinSpeed = 360;
let elapsed = 0;
let isRolling = true;

_.setMoveSpeedRate(0);
_.setJumpSpeedRate(0);

_.onReceive((messageType) => {
  if (messageType !== "spin-stop") {
    return;
  }

  isRolling = false;
  _.setMoveSpeedRate(1);
  _.setJumpSpeedRate(1);
}, { player: false, item: true });

_.onFrame((deltaTime) => {
  if (!isRolling) {
    return;
  }

  const position = _.getPosition();
  if (!position) {
    isRolling = false;
    _.setMoveSpeedRate(1);
    _.setJumpSpeedRate(1);
    _.log("Spin movement stopped because the player position is unavailable.");
    return;
  }

  _.setPosition(
    new Vector3(
      position.x + moveSpeed * deltaTime,
      position.y,
      position.z,
    ),
  );

  elapsed += deltaTime;
  _.setRotation(
    new Quaternion().setFromEulerAngles(
      new Vector3(0, elapsed * spinSpeed, 0),
    ),
  );
});
