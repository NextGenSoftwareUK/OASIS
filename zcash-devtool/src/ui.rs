use zcash_protocol::value::ZatBalance;

const COIN: u64 = 1_0000_0000;

pub(crate) fn format_zec(value: impl TryInto<ZatBalance>) -> String {
    let value = i64::from(
        value
            .try_into()
            .map_err(|_| ())
            .expect("Values are formattable"),
    );
    let abs_value = value.unsigned_abs();
    let abs_zec = abs_value / COIN;
    let frac = abs_value % COIN;
    let zec = if value.is_negative() {
        -(abs_zec as i64)
    } else {
        abs_zec as i64
    };
    format!("{zec:3}.{frac:08} ZEC")
}
