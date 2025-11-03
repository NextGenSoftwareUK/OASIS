function convertDate(timestamp) {
  const date = new Date(timestamp);

  const options = { month: 'short', day: '2-digit', year: 'numeric' };
  const formattedDate = date.toLocaleDateString('en-US', options);

  return formattedDate;
}

function convertTime(timestamp) {
  const date = new Date(timestamp);

  // Use UTC methods to avoid timezone issues
  let hours = date.getUTCHours();
  let minutes = date.getUTCMinutes();

  // Format minutes to always be two digits
  minutes = minutes < 10 ? '0' + minutes : minutes;

  let period = 'AM';

  if (hours >= 12) {
    period = 'PM';
    if (hours > 12) {
      hours -= 12;
    }
  }

  // Handle midnight (12:00 AM)
  if (hours === 0) {
    hours = 12;
  }

  const formattedTime = `${hours}:${minutes} ${period}`;

  return formattedTime;
}

function removeCommaFromNumber(numberWithCommas) {
  const number = parseInt(numberWithCommas.replace(/,/g, ''), 10);

  return number;
}

module.exports = {
  convertDate,
  convertTime,
  removeCommaFromNumber,
};
