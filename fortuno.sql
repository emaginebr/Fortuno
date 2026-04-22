-- ============================================================================
-- Fortuno — PostgreSQL schema
-- ----------------------------------------------------------------------------
-- Gerado a partir do mapeamento EF Core em Fortuno.Infra/Context/FortunoContext.cs.
-- Compatível com PostgreSQL 13+ (usa GENERATED ALWAYS AS IDENTITY).
-- Todas as colunas de data usam "timestamp without time zone" (UTC na aplicação).
-- Enums são persistidos como int (.HasConversion<int>()).
--
-- Ordem: tabelas-raiz primeiro, dependentes em seguida.
-- Constraints e nomes de índice exatamente como o EF emite.
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- fortuna_lotteries
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_lotteries (
    lottery_id            bigint          GENERATED ALWAYS AS IDENTITY,
    store_id              bigint          NOT NULL,
    name                  varchar(160)    NOT NULL,
    slug                  varchar(200)    NOT NULL,
    description_md        text            NOT NULL,
    rules_md              text            NOT NULL,
    privacy_policy_md     text            NOT NULL,
    ticket_price          numeric(12,2)   NOT NULL,
    total_prize_value     numeric(14,2)   NOT NULL,
    ticket_min            integer         NOT NULL DEFAULT 0,
    ticket_max            integer         NOT NULL DEFAULT 0,
    ticket_num_ini        bigint          NOT NULL DEFAULT 1,
    ticket_num_end        bigint          NOT NULL DEFAULT 0,
    number_type           integer         NOT NULL DEFAULT 0,
    number_value_min      integer         NOT NULL,
    number_value_max      integer         NOT NULL,
    referral_percent      real            NOT NULL DEFAULT 0,
    status                integer         NOT NULL DEFAULT 0,
    cancel_reason         varchar(1000)   NULL,
    cancelled_by_user_id  bigint          NULL,
    cancelled_at          timestamp without time zone NULL,
    created_at            timestamp without time zone NOT NULL DEFAULT now(),
    updated_at            timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_lotteries_pkey PRIMARY KEY (lottery_id)
);
CREATE UNIQUE INDEX fortuna_lotteries_slug_uq  ON fortuna_lotteries (slug);
CREATE        INDEX fortuna_lotteries_status_ix ON fortuna_lotteries (status);

COMMENT ON COLUMN fortuna_lotteries.number_type IS 'Fortuno.Domain.Enums.NumberType (0=Int64)';
COMMENT ON COLUMN fortuna_lotteries.status      IS 'Fortuno.Domain.Enums.LotteryStatus (0=Draft)';

-- ----------------------------------------------------------------------------
-- fortuna_lottery_images
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_lottery_images (
    lottery_image_id  bigint          GENERATED ALWAYS AS IDENTITY,
    lottery_id        bigint          NOT NULL,
    image_url         varchar(500)    NOT NULL,
    description       varchar(260)    NULL,
    display_order     integer         NOT NULL DEFAULT 0,
    created_at        timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_lottery_images_pkey PRIMARY KEY (lottery_image_id),
    CONSTRAINT fk_lottery_lottery_image
        FOREIGN KEY (lottery_id) REFERENCES fortuna_lotteries (lottery_id)
        ON DELETE NO ACTION
);

-- ----------------------------------------------------------------------------
-- fortuna_lottery_combos
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_lottery_combos (
    lottery_combo_id  bigint          GENERATED ALWAYS AS IDENTITY,
    lottery_id        bigint          NOT NULL,
    name              varchar(120)    NOT NULL,
    discount_value    real            NOT NULL DEFAULT 0,
    discount_label    varchar(80)     NOT NULL,
    quantity_start    integer         NOT NULL DEFAULT 0,
    quantity_end      integer         NOT NULL DEFAULT 0,
    created_at        timestamp without time zone NOT NULL DEFAULT now(),
    updated_at        timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_lottery_combos_pkey PRIMARY KEY (lottery_combo_id),
    CONSTRAINT fk_lottery_lottery_combo
        FOREIGN KEY (lottery_id) REFERENCES fortuna_lotteries (lottery_id)
        ON DELETE NO ACTION
);

-- ----------------------------------------------------------------------------
-- fortuna_tickets
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_tickets (
    ticket_id      bigint        GENERATED ALWAYS AS IDENTITY,
    lottery_id     bigint        NOT NULL,
    user_id        bigint        NOT NULL,
    invoice_id     bigint        NOT NULL,
    ticket_number  bigint        NOT NULL,
    ticket_value   varchar(64)   NOT NULL DEFAULT '',
    refund_state   integer       NOT NULL DEFAULT 0,
    created_at     timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_tickets_pkey PRIMARY KEY (ticket_id),
    CONSTRAINT fk_lottery_ticket
        FOREIGN KEY (lottery_id) REFERENCES fortuna_lotteries (lottery_id)
        ON DELETE NO ACTION
);
CREATE UNIQUE INDEX fortuna_tickets_lottery_number_uq ON fortuna_tickets (lottery_id, ticket_number);
CREATE        INDEX fortuna_tickets_lottery_refund_ix ON fortuna_tickets (lottery_id, refund_state);
CREATE        INDEX fortuna_tickets_user_created_ix   ON fortuna_tickets (user_id, created_at);
CREATE        INDEX fortuna_tickets_invoice_ix        ON fortuna_tickets (invoice_id);
CREATE        INDEX fortuna_tickets_lottery_value_ix  ON fortuna_tickets (lottery_id, ticket_value);

COMMENT ON COLUMN fortuna_tickets.refund_state IS 'Fortuno.Domain.Enums.TicketRefundState (0=None)';
COMMENT ON COLUMN fortuna_tickets.ticket_value IS 'Representação textual ordenada de ticket_number (Int64 → decimal; Composed → componentes 2-dígitos ordenados separados por "-", ex.: "05-11-28-39-60"). Populado na emissão via NumberCompositionService.Format.';

-- ----------------------------------------------------------------------------
-- fortuna_raffles
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_raffles (
    raffle_id                bigint          GENERATED ALWAYS AS IDENTITY,
    lottery_id               bigint          NOT NULL,
    name                     varchar(160)    NOT NULL,
    description_md           text            NULL,
    raffle_datetime          timestamp without time zone NOT NULL,
    video_url                varchar(500)    NULL,
    include_previous_winners boolean         NOT NULL DEFAULT false,
    status                   integer         NOT NULL DEFAULT 0,
    created_at               timestamp without time zone NOT NULL DEFAULT now(),
    updated_at               timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_raffles_pkey PRIMARY KEY (raffle_id),
    CONSTRAINT fk_lottery_raffle
        FOREIGN KEY (lottery_id) REFERENCES fortuna_lotteries (lottery_id)
        ON DELETE NO ACTION
);

COMMENT ON COLUMN fortuna_raffles.status IS 'Fortuno.Domain.Enums.RaffleStatus (0=Open)';

-- ----------------------------------------------------------------------------
-- fortuna_raffle_awards
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_raffle_awards (
    raffle_award_id  bigint          GENERATED ALWAYS AS IDENTITY,
    raffle_id        bigint          NOT NULL,
    position         integer         NOT NULL,
    description      varchar(300)    NOT NULL,
    CONSTRAINT fortuna_raffle_awards_pkey PRIMARY KEY (raffle_award_id),
    CONSTRAINT fk_raffle_raffle_award
        FOREIGN KEY (raffle_id) REFERENCES fortuna_raffles (raffle_id)
        ON DELETE NO ACTION
);
CREATE UNIQUE INDEX fortuna_raffle_awards_raffle_position_uq
    ON fortuna_raffle_awards (raffle_id, position);

-- ----------------------------------------------------------------------------
-- fortuna_raffle_winners
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_raffle_winners (
    raffle_winner_id  bigint          GENERATED ALWAYS AS IDENTITY,
    raffle_id         bigint          NOT NULL,
    raffle_award_id   bigint          NOT NULL,
    ticket_id         bigint          NULL,
    user_id           bigint          NULL,
    position          integer         NOT NULL,
    prize_text        varchar(300)    NOT NULL,
    created_at        timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_raffle_winners_pkey PRIMARY KEY (raffle_winner_id),
    CONSTRAINT fk_raffle_raffle_winner
        FOREIGN KEY (raffle_id) REFERENCES fortuna_raffles (raffle_id)
        ON DELETE NO ACTION,
    CONSTRAINT fk_raffle_award_raffle_winner
        FOREIGN KEY (raffle_award_id) REFERENCES fortuna_raffle_awards (raffle_award_id)
        ON DELETE NO ACTION,
    CONSTRAINT fk_ticket_raffle_winner
        FOREIGN KEY (ticket_id) REFERENCES fortuna_tickets (ticket_id)
        ON DELETE NO ACTION
);
CREATE UNIQUE INDEX fortuna_raffle_winners_raffle_award_uq
    ON fortuna_raffle_winners (raffle_id, raffle_award_id);

-- ----------------------------------------------------------------------------
-- fortuna_user_referrers
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_user_referrers (
    user_referrer_id  bigint      GENERATED ALWAYS AS IDENTITY,
    user_id           bigint      NOT NULL,
    referral_code     varchar(8)  NOT NULL,
    created_at        timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_user_referrers_pkey PRIMARY KEY (user_referrer_id)
);
CREATE UNIQUE INDEX fortuna_user_referrers_user_uq ON fortuna_user_referrers (user_id);
CREATE UNIQUE INDEX fortuna_user_referrers_code_uq ON fortuna_user_referrers (referral_code);

-- ----------------------------------------------------------------------------
-- fortuna_invoice_referrers
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_invoice_referrers (
    invoice_referrer_id           bigint   GENERATED ALWAYS AS IDENTITY,
    invoice_id                    bigint   NOT NULL,
    referrer_user_id              bigint   NOT NULL,
    lottery_id                    bigint   NOT NULL,
    referral_percent_at_purchase  real     NOT NULL,
    created_at                    timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_invoice_referrers_pkey PRIMARY KEY (invoice_referrer_id)
);
CREATE UNIQUE INDEX fortuna_invoice_referrers_invoice_uq
    ON fortuna_invoice_referrers (invoice_id);
CREATE        INDEX fortuna_invoice_referrers_referrer_lottery_ix
    ON fortuna_invoice_referrers (referrer_user_id, lottery_id);

-- ----------------------------------------------------------------------------
-- fortuna_refund_logs
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_refund_logs (
    refund_log_id        bigint          GENERATED ALWAYS AS IDENTITY,
    ticket_id            bigint          NOT NULL,
    executed_by_user_id  bigint          NOT NULL,
    reference_value      numeric(12,2)   NOT NULL,
    external_reference   varchar(160)    NULL,
    created_at           timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_refund_logs_pkey PRIMARY KEY (refund_log_id),
    CONSTRAINT fk_ticket_refund_log
        FOREIGN KEY (ticket_id) REFERENCES fortuna_tickets (ticket_id)
        ON DELETE NO ACTION
);

-- ----------------------------------------------------------------------------
-- fortuna_number_reservations
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_number_reservations (
    reservation_id  bigint   GENERATED ALWAYS AS IDENTITY,
    lottery_id      bigint   NOT NULL,
    user_id         bigint   NOT NULL,
    invoice_id      bigint   NULL,
    ticket_number   bigint   NOT NULL,
    expires_at      timestamp without time zone NOT NULL,
    created_at      timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_number_reservations_pkey PRIMARY KEY (reservation_id)
);
CREATE INDEX fortuna_number_reservations_lottery_expires_ix
    ON fortuna_number_reservations (lottery_id, expires_at);
CREATE INDEX fortuna_number_reservations_user_lottery_ix
    ON fortuna_number_reservations (user_id, lottery_id);

-- ----------------------------------------------------------------------------
-- fortuna_ticket_orders
-- ----------------------------------------------------------------------------
-- Adicionada pela feature 003-ticket-qrcode-purchase: persiste o contexto da
-- compra entre a criação do QR Code (POST /payment/qrcode no ProxyPay) e a
-- emissão dos tickets (disparada pelo polling de `CheckQRCodeStatus`).
--
-- Idempotência de pagamento é garantida via UPDATE condicional:
--   UPDATE fortuna_ticket_orders SET status = 2, updated_at = now()
--    WHERE ticket_order_id = @id AND status = 1
-- retornando o número de linhas afetadas (ver R-002 do plano).
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_ticket_orders (
    ticket_order_id           bigint          GENERATED BY DEFAULT AS IDENTITY,
    invoice_id                   bigint          NOT NULL,
    invoice_number               varchar(40)     NOT NULL,
    user_id                      bigint          NOT NULL,
    lottery_id                   bigint          NOT NULL,
    quantity                     integer         NOT NULL,
    mode                         integer         NOT NULL,
    referral_code                varchar(8)      NULL,
    referral_percent_at_purchase real            NOT NULL DEFAULT 0,
    total_amount                 numeric(14,2)   NOT NULL,
    br_code                      varchar(2000)   NULL,
    br_code_base64               text            NULL,
    expired_at                   timestamp without time zone NOT NULL,
    status                       integer         NOT NULL DEFAULT 1,
    created_at                   timestamp without time zone NOT NULL DEFAULT now(),
    updated_at                   timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_ticket_orders_pkey PRIMARY KEY (ticket_order_id),
    CONSTRAINT fk_ticket_order_lottery
        FOREIGN KEY (lottery_id) REFERENCES fortuna_lotteries (lottery_id)
        ON DELETE NO ACTION
);
CREATE UNIQUE INDEX ix_ticket_orders_invoice_id
    ON fortuna_ticket_orders (invoice_id);
CREATE        INDEX ix_ticket_orders_user_id
    ON fortuna_ticket_orders (user_id);
CREATE        INDEX ix_ticket_orders_lottery_id
    ON fortuna_ticket_orders (lottery_id);

COMMENT ON COLUMN fortuna_ticket_orders.mode   IS 'Fortuno.Domain.Enums.TicketOrderMode (1=Random, 2=UserPicks)';
COMMENT ON COLUMN fortuna_ticket_orders.status IS 'Fortuno.Domain.Enums.TicketOrderStatus — pareado com ProxyPay.InvoiceStatusEnum (1=Pending, 3=Paid, 5=Cancelled, 6=Expired; 2=Sent e 4=Overdue reservados no DTO)';

-- ----------------------------------------------------------------------------
-- fortuna_ticket_order_numbers
-- ----------------------------------------------------------------------------
-- Armazena os números escolhidos pelo comprador no modo UserPicks, um por linha.
-- Substitui o antigo campo `picked_numbers_json` em fortuna_ticket_orders
-- (normalização 1:N por TicketOrder).
-- ----------------------------------------------------------------------------
CREATE TABLE fortuna_ticket_order_numbers (
    ticket_order_number_id  bigint   GENERATED ALWAYS AS IDENTITY,
    ticket_order_id         bigint   NOT NULL,
    ticket_number           bigint   NOT NULL,
    created_at              timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fortuna_ticket_order_numbers_pkey PRIMARY KEY (ticket_order_number_id),
    CONSTRAINT fk_ticket_order_number_order
        FOREIGN KEY (ticket_order_id) REFERENCES fortuna_ticket_orders (ticket_order_id)
        ON DELETE NO ACTION
);
CREATE        INDEX ix_ticket_order_numbers_order_id
    ON fortuna_ticket_order_numbers (ticket_order_id);
CREATE UNIQUE INDEX ix_ticket_order_numbers_order_number_uq
    ON fortuna_ticket_order_numbers (ticket_order_id, ticket_number);

COMMIT;
