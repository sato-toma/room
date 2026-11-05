// const moveSpeed = 2.5;
const moveSpeed = 10;
const spinSpeed = 360;
let moveDirection = new Vector3(0, 0, 1);
let elapsed = 0;
let isRolling = true;

_.setMoveSpeedRate(0);
_.setJumpSpeedRate(0);

_.onReceive(
  (messageType, arg) => {
    if (messageType === "spin-direction" && arg instanceof Vector3) {
      const direction = new Vector3(arg.x, arg.y, arg.z);
      if (direction.lengthSq() > 0) {
        moveDirection = direction.normalize();
      }
      return;
    }

    if (messageType !== "spin-stop") {
      return;
    }

    isRolling = false;
    _.setMoveSpeedRate(1);
    _.setJumpSpeedRate(1);
  },
  { player: false, item: true },
);

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
      position.x + moveDirection.x * moveSpeed * deltaTime,
      position.y + moveDirection.y * moveSpeed * deltaTime,
      position.z + moveDirection.z * moveSpeed * deltaTime,
    ),
  );

  elapsed += deltaTime;
  _.setRotation(
    new Quaternion().setFromEulerAngles(new Vector3(0, elapsed * spinSpeed, 0)),
  );
});
