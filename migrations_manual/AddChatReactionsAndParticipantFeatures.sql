-- ============================================================
-- Миграция: AddChatReactionsAndParticipantFeatures
-- Применить к PostgreSQL базе DeliveryCRM
-- ============================================================

-- 1. Новые поля в таблице ChatMessages
ALTER TABLE "коммуникации"."ChatMessages"
    ADD COLUMN IF NOT EXISTS "ReplyToMessage_id" integer NULL,
    ADD COLUMN IF NOT EXISTS "MentionedUserIds"  varchar(1000) NULL,
    ADD COLUMN IF NOT EXISTS "DeliveryStatus"    smallint NOT NULL DEFAULT 0;

-- 2. Внешний ключ для Reply (self-reference)
ALTER TABLE "коммуникации"."ChatMessages"
    ADD CONSTRAINT IF NOT EXISTS "FK_ChatMessages_ReplyToMessage"
    FOREIGN KEY ("ReplyToMessage_id")
    REFERENCES "коммуникации"."ChatMessages" ("ID_ChatMessage")
    ON DELETE SET NULL;

-- 3. Новая таблица MessageReactions
CREATE TABLE IF NOT EXISTS "коммуникации"."MessageReactions" (
    "ID_MessageReaction" serial PRIMARY KEY,
    "ChatMessage_id"     integer NOT NULL,
    "User_id"            integer NOT NULL,
    "Emoji"              varchar(50) NOT NULL,
    "Created_at"         timestamp with time zone NOT NULL DEFAULT now(),

    CONSTRAINT "FK_MessageReactions_ChatMessage"
        FOREIGN KEY ("ChatMessage_id")
        REFERENCES "коммуникации"."ChatMessages" ("ID_ChatMessage")
        ON DELETE CASCADE,

    CONSTRAINT "FK_MessageReactions_User"
        FOREIGN KEY ("User_id")
        REFERENCES "пользователи_и_доступ"."Users" ("ID_User")
        ON DELETE CASCADE
);

-- 4. Уникальный индекс: один пользователь — одна реакция с одним emoji на сообщение
CREATE UNIQUE INDEX IF NOT EXISTS "UX_MessageReactions_Message_User_Emoji"
    ON "коммуникации"."MessageReactions" ("ChatMessage_id", "User_id", "Emoji");
