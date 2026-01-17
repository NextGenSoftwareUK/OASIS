function getCurrectActiveCar(cars) {
  const car = cars.filter(
    (carData) =>
      (carData.isActive === false && carData.isVerify === false) ||
      (carData.isActive === true && carData.isVerify === true)
  );

  return car;
}

module.exports = { getCurrectActiveCar };
