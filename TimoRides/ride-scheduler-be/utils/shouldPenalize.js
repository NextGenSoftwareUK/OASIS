const moment = require('moment');

function shouldPenalizeUser(createdAt, pickupDate, cancellationDate) {
  const createdAtMoment = moment(createdAt);
  const pickupDateMoment = moment(pickupDate);
  const cancellationDateMoment = moment(cancellationDate);

  // Calculate the interval between the creation and pickup date
  const interval = pickupDateMoment.diff(createdAtMoment);

  // Calculate the midpoint
  const midpoint = createdAtMoment.add(interval / 2);

  // Check if cancellation happened after the midpoint
  if (cancellationDateMoment.isAfter(midpoint)) {
    return true; // Penalize the user
  }
  return false; // Do not penalize the user
}

module.exports = shouldPenalizeUser;
