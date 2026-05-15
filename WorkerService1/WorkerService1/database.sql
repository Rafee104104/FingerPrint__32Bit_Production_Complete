CREATE TABLE BS.ATT
(
  USERID     NUMBER(10),
  CHECKTIME  DATE
);

/*
The worker inserts with an array-bound MERGE like this:

MERGE INTO BS.ATT dst
USING
(
    SELECT
        :USERID AS USERID,
        :CHECKTIME AS CHECKTIME,
    FROM DUAL
) src
ON
(
    dst.USERID = src.USERID
    AND dst.CHECKTIME = src.CHECKTIME
)
WHEN NOT MATCHED THEN
    INSERT
    (
        USERID,
        CHECKTIME
    )
    VALUES
    (
        src.USERID,
        src.CHECKTIME
    );


SELECT USERID,
       TO_CHAR(CHECKTIME, 'YYYY-MM-DD HH24:MI:SS') AS CHECKTIME
FROM BS.ATT
ORDER BY CHECKTIME DESC;
*/
