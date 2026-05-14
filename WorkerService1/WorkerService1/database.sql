/*
CREATE TABLE BS.CARD_ENTRY
(
  D_CARD     VARCHAR2(50 BYTE),
  T_CARD     VARCHAR2(50 BYTE),
  CARD_NO    NUMBER(15),
  ENTY_DATE  DATE,
  CHECKTIME  VARCHAR2(50 BYTE),
  MIN1       VARCHAR2(12 BYTE),
  MAX1       VARCHAR2(15 BYTE)
);

CREATE UNIQUE INDEX BS.UK_CARD_ENTRY_CARD_TIME
ON BS.CARD_ENTRY (CARD_NO, CHECKTIME);
*/
/*
The worker inserts with an array-bound MERGE like this:

MERGE INTO BS.CARD_ENTRY dst
USING
(
    SELECT
        :D_CARD AS D_CARD,
        :T_CARD AS T_CARD,
        :CARD_NO AS CARD_NO,
        :ENTY_DATE AS ENTY_DATE,
        :CHECKTIME AS CHECKTIME,
        :MIN1 AS MIN1,
        :MAX1 AS MAX1
    FROM DUAL
) src
ON
(
    dst.CARD_NO = src.CARD_NO
    AND dst.CHECKTIME = src.CHECKTIME
)
WHEN NOT MATCHED THEN
    INSERT
    (
        D_CARD,
        T_CARD,
        CARD_NO,
        ENTY_DATE,
        CHECKTIME,
        MIN1,
        MAX1
    )
    VALUES
    (
        src.D_CARD,
        src.T_CARD,
        src.CARD_NO,
        src.ENTY_DATE,
        src.CHECKTIME,
        src.MIN1,
        src.MAX1
    );


SELECT D_CARD,
       T_CARD,
       CARD_NO,
       TO_CHAR(ENTY_DATE, 'YYYY-MM-DD') AS ENTY_DATE,
       CHECKTIME,
       MIN1,
       MAX1
FROM BS.CARD_ENTRY
ORDER BY ENTY_DATE DESC, T_CARD DESC;
*/
