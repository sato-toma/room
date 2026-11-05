const horizontalMoveSpeed = 1;
const verticalMoveSpeed = 1;
const spinSpeed = 360;
const defaultGravity = -9.81;
let moveDirection = new Vector3(0, 0, 1);
let elapsed = 0;
let isRolling = true;

_.setMoveSpeedRate(0);
_.setJumpSpeedRate(0);
_.setGravity(0);

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
    _.setGravity(defaultGravity);
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
    _.setGravity(defaultGravity);
    _.log("Spin movement stopped because the player position is unavailable.");
    return;
  }

  _.setPosition(
    new Vector3(
      position.x + moveDirection.x * horizontalMoveSpeed * deltaTime,
      position.y + moveDirection.y * verticalMoveSpeed * deltaTime,
      position.z + moveDirection.z * horizontalMoveSpeed * deltaTime,
    ),
  );

  elapsed += deltaTime;
  _.setRotation(
    new Quaternion().setFromEulerAngles(new Vector3(0, elapsed * spinSpeed, 0)),
  );
});
