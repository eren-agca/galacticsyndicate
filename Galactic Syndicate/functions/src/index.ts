import {onCall, HttpsError} from "firebase-functions/v2/https";
import {logger} from "firebase-functions/v2";
import {initializeApp} from "firebase-admin/app";
import {getFirestore, FieldValue, Timestamp, FieldPath}
  from "firebase-admin/firestore";
import {onSchedule} from "firebase-functions/v2/scheduler";
import {getRemoteConfig} from "firebase-admin/remote-config";

// Firebase Admin SDK'sını başlat
initializeApp();
const db = getFirestore();

// --- Arayüz (Interface) Tanımlamaları ---
interface TransactionData {
  planetName: string;
  itemName: string;
}

interface InventorySlot {
  itemName: string;
  quantity: number;
}

interface UpgradeData {
  buffType: string;
}

interface MarketPricesRequest {
  planetName: string;
}

interface LeaderboardRequest {
  boardType: "players" | "syndicates";
  limit: number;
}

interface JoinSyndicateRequest {
  syndicateId: string;
}

interface CreateSyndicateRequest {
  name: string;
  tag: string;
  description: string;
}
interface EconomyItem {
  Supply: number;
  Demand: number;
}
interface SyndicateMembersRequest {
  syndicateId: string;
}

interface SetUsernameRequest {
  username: string;
}

// --- buyItem Fonksiyonu ---
export const buyItem = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;
  const data = request.data as TransactionData;

  if (!data || !data.planetName || !data.itemName) {
    const err = "Request must include planetName and itemName.";
    throw new HttpsError("invalid-argument", err);
  }
  const {planetName, itemName} = data;

  logger.info(`Buy req: ${itemName} on ${planetName} by UID: ${uid}`);

  try {
    const result = await db.runTransaction(async (transaction) => {
      const playerDocRef = db.collection("users").doc(uid);
      const economyDocRef = db.collection("economies").doc(planetName);
      const itemDataRef = db.collection("items").doc(itemName);
      const eventQuery = db.collection("market_events")
        .where("planetName", "==", planetName)
        .where("itemName", "==", itemName)
        .limit(1);

      const [playerDoc, economyDoc, itemDoc] = await transaction.getAll(
        playerDocRef, economyDocRef, itemDataRef,
      );
      const eventSnapshot = await transaction.get(eventQuery);

      let eventPriceMultiplier = 1.0;
      if (!eventSnapshot.empty) {
        const eventData = eventSnapshot.docs[0].data();
        if (eventData?.priceMultiplier) {
          eventPriceMultiplier = eventData.priceMultiplier;
          logger.info(`Event for ${itemName}: x${eventPriceMultiplier}`);
        }
      }

      if (!playerDoc.exists) {
        throw new HttpsError("not-found", "Player data not found.");
      }
      if (!economyDoc.exists) {
        throw new HttpsError("not-found", `No economy for ${planetName}.`);
      }
      if (!itemDoc.exists) {
        throw new HttpsError("not-found", `No base value for ${itemName}.`);
      }

      const playerData = playerDoc.data();
      const economyData = economyDoc.data();
      const itemData = itemDoc.data();

      if (!playerData || !economyData || !itemData) {
        throw new HttpsError("internal", "Failed to read document data.");
      }

      const itemBaseValue = itemData.baseValue as number;
      const itemsOnPlanet = economyData.Items as Record<string, EconomyItem>;

      if (!itemsOnPlanet?.[itemName]) {
        const errMsg = `${itemName} not on ${planetName}.`;
        throw new HttpsError("not-found", errMsg);
      }

      const ecoItem = itemsOnPlanet[itemName];
      const supply = ecoItem.Supply > 0 ? ecoItem.Supply : 1;
      const demandRatio = ecoItem.Demand / supply;
      const priceVolatility = 0.7;
      const priceModifier = 1.0 + (demandRatio - 1.0) * priceVolatility;
      const clampedModifier = Math.max(0.2, Math.min(5.0, priceModifier));
      const dynamicPrice = itemBaseValue * clampedModifier * 1.05 *
          eventPriceMultiplier;
      const buyPrice = Math.round(dynamicPrice);

      const currentCredits = (playerData.credits as number) || 0;
      if (currentCredits < buyPrice) {
        throw new HttpsError("failed-precondition", "Insufficient credits.");
      }

      const inventory: InventorySlot[] = playerData.inventory || [];
      const maxSlots = (playerData.maxInventorySlots as number) || 5;
      const itemInInventory = inventory.find((s) => s.itemName === itemName);
      if (!itemInInventory && inventory.length >= maxSlots) {
        throw new HttpsError("failed-precondition", "Inventory is full.");
      }

      let newInventory: InventorySlot[];
      if (itemInInventory) {
        newInventory = inventory.map((s) => {
          return s.itemName === itemName ?
            {...s, quantity: s.quantity + 1} : s;
        });
      } else {
        newInventory = [...inventory, {itemName, quantity: 1}];
      }

      transaction.update(playerDocRef, {
        credits: FieldValue.increment(-buyPrice),
        inventory: newInventory,
      });
      transaction.update(economyDocRef, {
        [`Items.${itemName}.Supply`]: FieldValue.increment(-1),
        [`Items.${itemName}.Demand`]: FieldValue.increment(1),
      });

      return {
        success: true,
        message: `Bought ${itemName} for ${buyPrice}c.`,
        newCredits: currentCredits - buyPrice,
      };
    });

    const logMsg = `Purchase OK for ${uid}. New credits: ${result.newCredits}`;
    logger.info(logMsg);
    return result;
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    logger.error("buyItem function error:", {errorDetails: error});
    throw new HttpsError("internal", "Internal server error.");
  }
});

// --- sellItem Fonksiyonu ---
export const sellItem = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;
  const data = request.data as TransactionData;

  if (!data || !data.planetName || !data.itemName) {
    const err = "Request must include planetName and itemName.";
    throw new HttpsError("invalid-argument", err);
  }
  const {planetName, itemName} = data;

  logger.info(`Sell req: ${itemName} on ${planetName} by UID: ${uid}`);

  try {
    const result = await db.runTransaction(async (transaction) => {
      const playerDocRef = db.collection("users").doc(uid);
      const economyDocRef = db.collection("economies").doc(planetName);
      const itemDataRef = db.collection("items").doc(itemName);
      const eventQuery = db.collection("market_events")
        .where("planetName", "==", planetName)
        .where("itemName", "==", itemName)
        .limit(1);

      const [playerDoc, economyDoc, itemDoc] = await transaction.getAll(
        playerDocRef, economyDocRef, itemDataRef,
      );
      const eventSnapshot = await transaction.get(eventQuery);

      let eventPriceMultiplier = 1.0;
      if (!eventSnapshot.empty) {
        const eventData = eventSnapshot.docs[0].data();
        if (eventData?.priceMultiplier) {
          eventPriceMultiplier = eventData.priceMultiplier;
          logger.info(`Event for ${itemName}: x${eventPriceMultiplier}`);
        }
      }

      if (!playerDoc.exists) {
        throw new HttpsError("not-found", "Player data not found.");
      }
      if (!economyDoc.exists) {
        throw new HttpsError("not-found", `No economy for ${planetName}.`);
      }
      if (!itemDoc.exists) {
        throw new HttpsError("not-found", `No base value for ${itemName}.`);
      }

      const playerData = playerDoc.data();
      const economyData = economyDoc.data();
      const itemData = itemDoc.data();
      if (!playerData || !economyData || !itemData) {
        throw new HttpsError("internal", "Failed to read document data.");
      }

      const inventory: InventorySlot[] = playerData.inventory || [];
      const itemInInventory = inventory.find((s) => s.itemName === itemName);
      if (!itemInInventory || itemInInventory.quantity < 1) {
        const errMsg = `No ${itemName} in inventory.`;
        throw new HttpsError("failed-precondition", errMsg);
      }

      const itemsOnPlanet = economyData.Items as Record<string, EconomyItem>;
      if (!itemsOnPlanet?.[itemName]) {
        const errMsg = `${itemName} not traded on ${planetName}.`;
        throw new HttpsError("not-found", errMsg);
      }

      const ecoItem = itemsOnPlanet[itemName];
      const supply = ecoItem.Supply > 0 ? ecoItem.Supply : 1;
      const demandRatio = ecoItem.Demand / supply;
      const priceVolatility = 0.7;
      const priceModifier = 1.0 + (demandRatio - 1.0) * priceVolatility;
      const clampedModifier = Math.max(0.2, Math.min(5.0, priceModifier));
      const dynamicPrice = itemData.baseValue * clampedModifier * 0.90 *
          eventPriceMultiplier;
      const sellPrice = Math.round(dynamicPrice);

      let playerEarnings = sellPrice;
      let taxAmount = 0;
      const syndicateId = playerData.syndicateId as string | undefined;
      let tradeBonusMultiplier = 1.0;

      if (syndicateId) {
        const syndicateDocRef = db.collection("syndicates").doc(syndicateId);
        const syndicateDoc = await transaction.get(syndicateDocRef);
        if (syndicateDoc.exists) {
          const syndicateData = syndicateDoc.data();
          if (syndicateData) {
            const tradeBuffLevel =
                (syndicateData.TradeBuffLevel as number) || 0;
            tradeBonusMultiplier = 1.0 + (tradeBuffLevel * 0.02);
            taxAmount = Math.floor(sellPrice * 0.05);
            playerEarnings = sellPrice - taxAmount;
          }
        }
      }

      playerEarnings = Math.round(playerEarnings * tradeBonusMultiplier);
      const finalCredits = (playerData.credits as number || 0) +
          playerEarnings;

      const newInventory = inventory
        .map((s) => s.itemName === itemName ?
          {...s, quantity: s.quantity - 1} : s)
        .filter((s) => s.quantity > 0);

      transaction.update(playerDocRef, {
        credits: FieldValue.increment(playerEarnings),
        inventory: newInventory,
      });
      transaction.update(economyDocRef, {
        [`Items.${itemName}.Supply`]: FieldValue.increment(1),
        [`Items.${itemName}.Demand`]: FieldValue.increment(-1),
      });

      if (syndicateId && taxAmount > 0) {
        const syndicateRef = db.collection("syndicates").doc(syndicateId);
        transaction.update(syndicateRef, {
          Treasury: FieldValue.increment(taxAmount),
        });
      }

      const msgParts = [
        `Sold ${itemName} for ${sellPrice}c`, `(+${playerEarnings}c).`,
      ];
      const finalMessage = msgParts.join(" ");

      return {
        success: true,
        message: finalMessage,
        newCredits: finalCredits,
      };
    });

    const logMsg = `Sale OK for ${uid}. New credits: ${result.newCredits}`;
    logger.info(logMsg);
    return result;
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    logger.error("sellItem function error:", {errorDetails: error});
    throw new HttpsError("internal", "Internal server error.");
  }
});

// --- purchaseSyndicateUpgrade Fonksiyonu ---
export const purchaseSyndicateUpgrade = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const playerId = request.auth.uid;
  const data = request.data as UpgradeData;

  if (!data || data.buffType !== "trade") {
    const err = "Invalid or missing buffType specified.";
    throw new HttpsError("invalid-argument", err);
  }
  const {buffType} = data;

  logger.info(
    `Upgrade req from UID: ${playerId} for buff: ${buffType}`,
  );

  try {
    const result = await db.runTransaction(async (transaction) => {
      const playerDocRef = db.collection("users").doc(playerId);
      const playerDoc = await transaction.get(playerDocRef);
      if (!playerDoc.exists) {
        throw new HttpsError("not-found", "Player data not found.");
      }

      const playerData = playerDoc.data();
      if (!playerData) {
        throw new HttpsError("internal", "Could not read player data.");
      }

      const syndicateId = playerData.syndicateId as string | undefined;
      if (!syndicateId) {
        const errMsg = "Player is not in a syndicate.";
        throw new HttpsError("failed-precondition", errMsg);
      }

      const syndicateDocRef = db.collection("syndicates").doc(syndicateId);
      const syndicateDoc = await transaction.get(syndicateDocRef);
      if (!syndicateDoc.exists) {
        const errMsg = `Syndicate ${syndicateId} not found.`;
        throw new HttpsError("not-found", errMsg);
      }

      const syndicateData = syndicateDoc.data();
      if (!syndicateData) {
        throw new HttpsError("internal", "Could not read syndicate data.");
      }

      if (syndicateData.LeaderID !== playerId) {
        const msg = "Only the leader can purchase upgrades.";
        throw new HttpsError("permission-denied", msg);
      }

      const currentLevel = (syndicateData.TradeBuffLevel as number) || 0;
      const upgradeCost = Math.round(5000 * Math.pow(2.5, currentLevel));
      const treasury = (syndicateData.Treasury as number) || 0;
      if (treasury < upgradeCost) {
        const msg = "Insufficient funds in treasury.";
        throw new HttpsError("failed-precondition", msg);
      }

      transaction.update(syndicateDocRef, {
        Treasury: FieldValue.increment(-upgradeCost),
        TradeBuffLevel: FieldValue.increment(1),
      });

      return {success: true, message: "Upgrade purchased successfully."};
    });

    logger.info(`Upgrade successful: ${result.message}`);
    return result;
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    logger.error("purchaseSyndicateUpgrade error:", {errorDetails: error});
    throw new HttpsError("internal", "Internal server error.");
  }
});

// --- handleNewGame Fonksiyonu ---
export const handleNewGame = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;
  logger.info(`New game request from UID: ${uid}. Cleaning up...`);

  try {
    await db.runTransaction(async (transaction) => {
      const playerDocRef = db.collection("users").doc(uid);
      const playerDoc = await transaction.get(playerDocRef);

      if (playerDoc.exists && playerDoc.data()?.syndicateId) {
        const syndicateId = playerDoc.data()?.syndicateId as string;
        const syndicateDocRef = db.collection("syndicates").doc(syndicateId);
        const syndicateDoc = await transaction.get(syndicateDocRef);

        if (syndicateDoc.exists) {
          transaction.update(syndicateDocRef, {
            MemberIDs: FieldValue.arrayRemove(uid),
          },
          );
          const syndicateData = syndicateDoc.data();
          if (syndicateData?.MemberIDs) {
            const memberList = syndicateData.MemberIDs as string[];
            if (memberList.length <= 1) {
              const delMsg = `Deleting syndicate ${syndicateId} (last member).`;
              logger.info(delMsg);
              transaction.delete(syndicateDocRef);
            }
          }
        }
      }
      transaction.delete(playerDocRef);
    });

    logger.info(`Cleanup successful for UID: ${uid}.`);
    return {success: true, message: "New game started successfully."};
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    const logMsg = `Error in handleNewGame for UID: ${uid}.`;
    logger.error(logMsg, {errorDetails: error});
    throw new HttpsError("internal", "Internal server error.");
  }
});

// --- getMarketPrices Fonksiyonu ---
export const getMarketPrices = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const data = request.data as MarketPricesRequest;
  if (!data || typeof data.planetName !== "string") {
    const err = "Request must include a planetName string.";
    throw new HttpsError("invalid-argument", err);
  }
  const {planetName} = data;

  logger.info(`Fetching market prices for planet: ${planetName}`);

  try {
    const itemsSnapshot = await db.collection("items").get();
    const itemBaseValues: {[key: string]: number} = {};
    itemsSnapshot.forEach((doc) => {
      itemBaseValues[doc.id] = doc.data().baseValue;
    });

    const logMessage = "Base values loaded from 'items' collection.";
    logger.info(logMessage, {itemKeys: Object.keys(itemBaseValues)});

    const prices = await db.runTransaction(async (transaction) => {
      const economyDocRef = db.collection("economies").doc(planetName);
      const eventsQuery = db.collection("market_events")
        .where("planetName", "==", planetName);

      const [economyDoc, eventsSnapshot] = await Promise.all([
        transaction.get(economyDocRef),
        transaction.get(eventsQuery),
      ]);


      if (!economyDoc.exists) {
        const errParts = [
          `Economy for planet '${planetName}' does not exist.`,
          "Please run the 'Initialize All Economies' tool in Unity.",
        ];
        throw new HttpsError("not-found", errParts.join(" "));
      }
      const economyData = economyDoc.data();

      const activeEvents: {[key: string]: number} = {};
      eventsSnapshot.forEach((doc) => {
        const event = doc.data();
        if (event.itemName && event.priceMultiplier) {
          activeEvents[event.itemName] = event.priceMultiplier;
        }
      });

      const calculatedPrices:
          {[p: string]: {buyPrice: number, sellPrice: number}} = {};
      const itemsOnPlanet = economyData?.Items as
          Record<string, EconomyItem>;
      if (!itemsOnPlanet) {
        logger.warn(`Economy for ${planetName} has no 'Items' map.`);
        return {};
      }

      // --- NİHAİ DÜZELTME: for...in -> Object.keys().forEach() ---
      // Bu, Firestore'dan gelen nesnelerle güvenli çalışmayı garantiler.
      Object.keys(itemsOnPlanet).forEach((itemName) => {
        // Base value kontrolü ve detaylı loglama
        if (!itemBaseValues[itemName]) {
          const msgParts = [
            `[MISMATCH] No baseValue for item: '${itemName}'.`,
            "Check for spaces/case differences in names.",
          ];
          logger.warn(msgParts.join(" "));
          return; // forEach içinde 'continue' yerine 'return' kullanılır.
        }
        const ecoItem = itemsOnPlanet[itemName];
        const baseValue = itemBaseValues[itemName];
        const eventMultiplier = activeEvents[itemName] || 1.0;
        const supply = ecoItem.Supply > 0 ? ecoItem.Supply : 1;
        const demandRatio = ecoItem.Demand / supply;
        // --- DENGELEME AYARI 1: Fiyat Oynaklığı Azaltıldı ---
        // Fiyatların arz/talep değişimlerine daha
        // yumuşak tepki vermesini sağlar.
        const priceVolatility = 0.5;
        const priceModifier = 1.0 + (demandRatio - 1.0) * priceVolatility;
        // --- DENGELEME AYARI 2: Fiyat Makası Daraltıldı ---
        // Fiyatın en fazla %250'ye çıkıp, en az %50'ye düşmesini sağlar.
        const clampedModifier = Math.max(0.5, Math.min(2.5, priceModifier));
        const dynamicPrice = baseValue * clampedModifier * eventMultiplier;
        const buyPrice = Math.round(dynamicPrice * 1.05);
        const sellPrice = Math.round(dynamicPrice * 0.90);
        calculatedPrices[itemName] = {buyPrice, sellPrice};
      });

      return calculatedPrices;
    });

    return {success: true, prices: prices};
  } catch (error: unknown) {
    if (error instanceof HttpsError) {
      const logMessage =
          `Controlled error in getMarketPrices for ${planetName}:`;
      logger.error(
        logMessage,
        {code: error.code, message: error.message},
      );
      throw error;
    }

    // Güvenli hata loglama için değişkenleri hazırla
    let errorMessage = "An unknown internal error occurred.";
    let errorStack = "Stack trace not available.";
    let rawErrorString = "Could not stringify the raw error object.";

    // Orijinal hatayı güvenli bir şekilde metne dönüştürmeyi dene
    try {
      rawErrorString = JSON.stringify(
        error, Object.getOwnPropertyNames(error),
      );
    } catch (e) {
      const rawErrorMsg = [
        "The raw error object could not be processed ",
        "due to its complex structure.",
      ];
      rawErrorString = rawErrorMsg.join("");
    }

    if (error instanceof Error) {
      errorMessage = error.message;
      errorStack = error.stack ?? "No stack trace available.";
    }

    const crashMessage =
        `!!! UNEXPECTED CRASH in getMarketPrices for ${planetName} !!!`;

    logger.error(crashMessage, {
      planet: planetName,
      errorMessage,
      stack: errorStack,
      rawErrorString,
    });

    const message = [
      `Server crashed for planet ${planetName}:`,
      errorMessage,
    ].join(" ");
    const details = {stack: errorStack, details: rawErrorString};
    throw new HttpsError("internal", message, details);
  }
});

// --- testConnection Fonksiyonu ---
export const testConnection = onCall((request) => {
  const uid = request.auth?.uid || "anonymous";
  const data = request.data;
  logger.info(`Test function called by ${uid} with data:`, {data});
  return {
    success: true,
    message: "Connection working!",
    receivedData: data,
    timestamp: new Date().toISOString(),
    userID: uid,
  };
});

// --- YENİ FONKSİYON: getLeaderboards ---
export const getLeaderboards = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const data = request.data as LeaderboardRequest;

  // Güvenlik: İstemcinin isteyebileceği maksimum limit 50 ile sınırlandırıldı.
  const limit = (data.limit > 0 && data.limit <= 50) ? data.limit : 20;

  if (data.boardType === "players") {
    logger.info(`Fetching top ${limit} players by credits.`);
    const playersSnapshot = await db.collection("users")
      .orderBy("credits", "desc")
      .limit(limit)
      .get();

    // Dökümanları daha basit bir formata dönüştür.
    const players = playersSnapshot.docs.map((doc) => {
      const playerData = doc.data();
      return {
        // Henüz bir kullanıcı adı sistemimiz olmadığı için
        // geçici olarak UID'nin bir kısmını kullanıyoruz.
        name: playerData.username || `Pilot-${doc.id.substring(0, 5)}`,
        value: playerData.credits || 0,
      };
    });
    return {success: true, leaderboard: players};
  }

  if (data.boardType === "syndicates") {
    logger.info(`Fetching top ${limit} syndicates by treasury.`);
    const syndicatesSnapshot = await db.collection("syndicates")
      .orderBy("Treasury", "desc")
      .limit(limit)
      .get();

    // Dökümanları daha basit bir formata dönüştür.
    const syndicates = syndicatesSnapshot.docs.map((doc) => {
      const syndicateData = doc.data();
      return {
        name: `[${syndicateData.Tag}] ${syndicateData.SyndicateName}`,
        value: syndicateData.Treasury || 0,
      };
    });
    return {success: true, leaderboard: syndicates};
  }

  // Eğer boardType 'players' veya 'syndicates' değilse hata fırlat.
  throw new HttpsError(
    "invalid-argument",
    "Invalid boardType. Must be 'players' or 'syndicates'."
  );
});

// --- YENİ FONKSİYON: getPublicSyndicates ---
// Oyuncuların katılabileceği sendikaların bir listesini döndürür.
export const getPublicSyndicates = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }

  try {
    // Şimdilik tüm sendikaları çekiyoruz, gelecekte sayfalama eklenebilir.
    const syndicatesSnapshot = await db.collection("syndicates")
      .orderBy("Treasury", "desc")
      .limit(50).get();

    const publicList = syndicatesSnapshot.docs.map((doc) => {
      const data = doc.data();
      return {
        id: doc.id,
        name: data.SyndicateName || "İsimsiz Sendika",
        tag: data.Tag || "???",
        memberCount: (data.MemberIDs || []).length,
      };
    });

    return {success: true, syndicates: publicList};
  } catch (error) {
    logger.error("Error in getPublicSyndicates:", {errorDetails: error});
    throw new HttpsError(
      "internal",
      "Sendika listesi çekilirken bir hata oluştu."
    );
  }
});

// --- YENİ FONKSİYON: getSyndicateMembers ---
// Bir sendikanın üye listesini ve detaylarını döndürür.
export const getSyndicateMembers = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;
  const data = request.data as SyndicateMembersRequest;

  if (!data.syndicateId) {
    throw new HttpsError("invalid-argument", "Syndicate ID is required.");
  }

  const syndicateDocRef = db.collection("syndicates").doc(data.syndicateId);
  const syndicateDoc = await syndicateDocRef.get();

  if (!syndicateDoc.exists) {
    throw new HttpsError("not-found", "Syndicate not found.");
  }

  const syndicateData = syndicateDoc.data();
  const memberIds = syndicateData?.MemberIDs as string[] || [];

  // Güvenlik: Sadece sendika üyeleri üye listesini görebilir.
  if (!memberIds.includes(uid)) {
    throw new HttpsError(
      "permission-denied",
      "You are not a member of this syndicate."
    );
  }

  if (memberIds.length === 0) {
    return {success: true, members: []};
  }

  // Her bir üyenin kullanıcı dökümanını çek.
  const memberDocs = await db.getAll(
    ...memberIds.map((id) => db.collection("users").doc(id))
  );

  const memberList = memberDocs.map((doc) => {
    const playerData = doc.data();
    const username = playerData?.username || `Pilot-${doc.id.substring(0, 5)}`;
    const profilePictureUrl = playerData?.profilePictureUrl || null;
    return {
      uid: doc.id,
      name: username,
      isLeader: doc.id === syndicateData?.LeaderID,
      profilePictureUrl: profilePictureUrl,
    };
  });

  return {success: true, members: memberList};
});

// --- YENİ FONKSİYON: joinSyndicate ---
// Bir oyuncunun belirtilen sendikaya katılmasını sağlar.
export const joinSyndicate = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;
  const data = request.data as JoinSyndicateRequest;

  if (!data.syndicateId) {
    throw new HttpsError("invalid-argument", "Syndicate ID zorunludur.");
  }

  logger.info(
    `Join syndicate req from UID: ${uid} for syndicate: ${data.syndicateId}`
  );

  try {
    const result = await db.runTransaction(async (transaction) => {
      const playerDocRef = db.collection("users").doc(uid);
      const syndicateDocRef = db.collection("syndicates").doc(data.syndicateId);

      const [playerDoc, syndicateDoc] = await transaction.getAll(
        playerDocRef, syndicateDocRef
      );

      if (!syndicateDoc.exists) {
        throw new HttpsError(
          "not-found",
          "Katılmak istenen sendika bulunamadı."
        );
      }

      if (playerDoc.exists && playerDoc.data()?.syndicateId) {
        throw new HttpsError(
          "failed-precondition",
          "Zaten bir sendikadasınız."
        );
      }

      // Oyuncuyu sendikanın üye listesine ekle
      transaction.update(syndicateDocRef, {
        MemberIDs: FieldValue.arrayUnion(uid),
      });

      // Oyuncunun kendi dökümanına sendika ID'sini yaz
      transaction.set(
        playerDocRef,
        {syndicateId: data.syndicateId},
        {merge: true}
      );

      return {success: true, message: "Sendikaya başarıyla katıldınız."};
    });

    return result;
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    logger.error("joinSyndicate function error:", {errorDetails: error});
    throw new HttpsError(
      "internal",
      "Sendikaya katılırken bir sunucu hatası oluştu."
    );
  }
});

// --- GÜNCELLEME: createSyndicate fonksiyonunu daha
// güvenli hale getiriyoruz ---
// Bu fonksiyon, bir önceki adımdaki createSyndicate
// fonksiyonunun
// daha sağlam ve güvenli halidir.
export const createSyndicate = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;

  // Gelen veriyi doğrula
  const data = request.data as CreateSyndicateRequest;
  const syndicateName = data.name as string;
  const tag = data.tag as string;
  const description = data.description as string;

  if (!syndicateName || !tag) {
    throw new HttpsError(
      "invalid-argument",
      "Sendika adı ve etiketi zorunludur."
    );
  }

  // --- İSİM UZUNLUĞU KONTROLÜ ---
  if (syndicateName.length > 12) {
    throw new HttpsError(
      "invalid-argument",
      "Sendika adı 12 karakterden uzun olamaz."
    );
  }
  // Etiket uzunluğu kontrolü (örnek: 2-4 karakter)
  if (tag.length < 2 || tag.length > 4) {
    throw new HttpsError(
      "invalid-argument",
      "Etiket 2 ile 4 karakter arasında olmalıdır."
    );
  }

  logger.info(
    `Create syndicate req from UID: ${uid} for name: ${syndicateName}`
  );

  try {
    const result = await db.runTransaction(async (transaction) => {
      const playerDocRef = db.collection("users").doc(uid);
      const playerDoc = await transaction.get(playerDocRef);

      if (playerDoc.exists && playerDoc.data()?.syndicateId) {
        throw new HttpsError(
          "failed-precondition",
          "Oyuncu zaten bir sendikada."
        );
      }

      const newSyndicateData = {
        SyndicateName: syndicateName,
        Tag: tag,
        Description: description || "",
        LeaderID: uid,
        MemberIDs: [uid],
        Treasury: 0,
        TradeBuffLevel: 0,
        EmblemURL: "",
      };

      const newSyndicateRef = db.collection("syndicates").doc();
      transaction.set(newSyndicateRef, newSyndicateData);
      transaction.set(
        playerDocRef,
        {syndicateId: newSyndicateRef.id},
        {merge: true}
      );

      const message = `Sendika '${syndicateName}' oluşturuldu.`;
      return {
        success: true, message, syndicateId: newSyndicateRef.id,
        syndicateData: newSyndicateData,
      };
    });
    return result;
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    logger.error("createSyndicate function error:", {errorDetails: error});
    throw new HttpsError(
      "internal",
      "Sendika oluşturulurken bir sunucu hatası oluştu."
    );
  }
});

// --- YENİ FONKSİYON: updateGalaxyNews ---
// Her 24 saatte bir çalışarak
// "Günün Haberleri"ni ve pazar etkinliğini günceller.
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export const updateGalaxyNews = onSchedule("every 24 hours", async (event) => {
  logger.info("Scheduled news update started.");

  try {
    // 1. Gerekli verileri Firestore'dan çek
    const itemsDoc = await db.collection("game_definitions").doc("items").get();
    const planetsDoc = await db.collection("game_definitions")
      .doc("planets").get();

    if (!itemsDoc.exists || !planetsDoc.exists) {
      const msg = "Game definitions not found in Firestore. Aborting.";
      logger.error(msg);
      throw new Error(msg);
    }

    const allItemNames = itemsDoc.data()?.allItemNames as string[] || [];
    const allPlanetNames = planetsDoc.data()?.allPlanetNames as string[] || [];

    if (allItemNames.length === 0 || allPlanetNames.length === 0) {
      const msg = "Item or planet name list is empty. Aborting.";
      logger.error(msg);
      throw new Error(msg);
    }

    // 2. Rastgele bir gezegen ve ürün seç
    const targetPlanet = allPlanetNames[
      Math.floor(Math.random() * allPlanetNames.length)
    ];
    const targetItem = allItemNames[
      Math.floor(Math.random() * allItemNames.length)
    ];

    // 3. Haber metni şablonlarını tanımla
    const newsTemplates = [
      // eslint-disable-next-line max-len
      "GÜNÜN HABERİ: {planetName} gezegeninde {itemName} kıtlığı yaşanıyor, fiyatlar tavan yaptı!",
      // eslint-disable-next-line max-len
      "ACİL ÇAĞRI: {planetName} sistemindeki endüstriyel talep nedeniyle {itemName} fiyatları fırladı!",
      // eslint-disable-next-line max-len
      "TÜCCARLARA DUYURU: {planetName} valiliği, {itemName} getiren pilotlara prim ödeyeceğini açıkladı!",
    ];

    const newsTemplate = newsTemplates[
      Math.floor(Math.random() * newsTemplates.length)
    ];
    const newsMessage = newsTemplate
      .replace("{planetName}", targetPlanet)
      .replace("{itemName}", targetItem);

    // 4. Pazar etkinliğini oluştur
    const priceMultiplier = 1.75; // Fiyatları %75 artır

    // Önceki tüm pazar etkinliklerini temizle
    const eventsCollection = db.collection("market_events");
    const oldEventsSnapshot = await eventsCollection.get();
    const batch = db.batch();
    oldEventsSnapshot.docs.forEach((doc) => batch.delete(doc.ref));
    await batch.commit();
    logger.info(`Deleted ${oldEventsSnapshot.size} old market events.`);

    // Yeni etkinliği ekle
    await eventsCollection.add({
      planetName: targetPlanet,
      itemName: targetItem,
      priceMultiplier: priceMultiplier,
      expiresAt: Timestamp.fromMillis(Date.now() + 24 * 60 * 60 * 1000),
    });
    logger.info(`Created new event for ${targetItem} on ${targetPlanet}.`);

    // 5. Remote Config'i güncelle
    const remoteConfig = getRemoteConfig();
    const template = await remoteConfig.getTemplate();
    template.parameters["galaxy_news"] = {defaultValue: {value: newsMessage}};
    await remoteConfig.publishTemplate(template);
    logger.info(`Remote Config updated with news: "${newsMessage}"`);
  } catch (error) {
    logger.error("Error in updateGalaxyNews scheduled function:", error);
  }
});

// --- YENİ FONKSİYON: setPlayerUsername ---
export const setPlayerUsername = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const uid = request.auth.uid;
  const data = request.data as SetUsernameRequest;
  const newUsername = data.username;

  // 1. Girdi Doğrulama
  if (!newUsername || newUsername.length < 3 || newUsername.length > 15) {
    throw new HttpsError(
      "invalid-argument",
      "Kullanıcı adı 3 ile 15 karakter arasında olmalıdır."
    );
  }
  if (!/^[a-zA-Z0-9_]+$/.test(newUsername)) {
    throw new HttpsError(
      "invalid-argument",
      "Kullanıcı adı sadece harf, rakam ve alt çizgi içerebilir."
    );
  }

  const lowerCaseUsername = newUsername.toLowerCase();
  const usernameDocRef = db.collection("usernames").doc(lowerCaseUsername);
  const playerDocRef = db.collection("users").doc(uid);

  logger.info(`Username change req: '${newUsername}' by UID: ${uid}`);

  // 2. Transaction ile Atomik İşlem
  try {
    await db.runTransaction(async (transaction) => {
      const usernameDoc = await transaction.get(usernameDocRef);
      const playerDoc = await transaction.get(playerDocRef);

      // Bu kullanıcı adı zaten alınmış mı ve başkasına mı ait?
      if (usernameDoc.exists && usernameDoc.data()?.uid !== uid) {
        throw new HttpsError(
          "already-exists",
          `'${newUsername}' kullanıcı adı zaten alınmış.`
        );
      }

      // DÜZELTME: Oyuncu dökümanı varsa güncelle, yoksa oluştur.
      if (playerDoc.exists) {
        // Mevcut oyuncuyu güncelle
        const oldUsername = playerDoc.data()?.username as string | undefined;
        if (oldUsername && oldUsername.toLowerCase() !== lowerCaseUsername) {
          const oldUsernameDocRef =
              db.collection("usernames").doc(oldUsername.toLowerCase());
          transaction.delete(oldUsernameDocRef);
        }
        transaction.update(playerDocRef, {username: newUsername});
      } else {
        // Yeni oyuncu için döküman oluştur.
        // Bu, "veri bulunamadı" hatasını çözer.
        transaction.set(playerDocRef, {username: newUsername});
      }

      // Yeni kullanıcı adını 'usernames'
      // koleksiyonuna ekle
      transaction.set(usernameDocRef, {uid: uid});
    });

    logger.info(`Username for ${uid} successfully set to ${newUsername}`);
    return {success: true, message: "Kullanıcı adı başarıyla ayarlandı!"};
  } catch (error) {
    if (error instanceof HttpsError) throw error;
    logger.error("setPlayerUsername function error:", {errorDetails: error});
    throw new HttpsError("internal", "Kullanıcı adı ayarlanırken " +
        "bir sunucu hatası oluştu.");
  }
});

// --- YENİ FONKSİYON: getGhostPlayers ---
// Diğer oyuncuların "hayaletlerini" göstermek için rastgele bir oyuncu
// örneklemi döndürür.
export const getGhostPlayers = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Authentication required.");
  }
  const requesterUid = request.auth.uid;

  try {
    // --- YENİ ADIM: Aktif olmayan oyuncuları filtrele ---
    // Son 60 saniye içinde görülmeyen oyuncuları "çevrimdışı" kabul et.
    // Bu, oyunda olmayan hayaletlerin görünmesini engeller.
    const activeThreshold = 60 * 1000; // 60 saniye (milisaniye cinsinden)
    const cutoffTimestamp = Timestamp.fromMillis(Date.now() - activeThreshold);

    const snapshot = await db.collection("users")
      .where(FieldPath.documentId(), "!=", requesterUid)
      // YENİ FİLTRE: Sadece son 60 saniyede aktif olan oyuncuları çek.
      // Bu sorgunun çalışması için Firestore'da bir index gerekebilir.
      // Hata alırsanız, hata mesajındaki linke tıklayarak index'i oluşturun.
      .where("lastSeen", ">", cutoffTimestamp)
      .get();

    if (snapshot.empty) {
      // Başka oyuncu yoksa boş bir liste döndür. Bu bir hata değildir.
      return {success: true, players: []};
    }

    // --- YENİ LOGLAMA ADIMI ---
    // Ne kadar oyuncu bulduğumuzu ve veritabanından ne geldiğini görelim.
    logger.info(`Found ${snapshot.size} potential ghost players.`);
    // -------------------------

    const players = snapshot.docs
      .map((doc) => {
        const data = doc.data();
        // Oyuncunun konum verisi yoksa onu listeye ekleme
        if (!data.playerPosition || !data.playerRotation) {
          return null;
        }
        return {
          uid: doc.id,
          username: data.username || `Pilot-${doc.id.substring(0, 5)}`,
          position: data.playerPosition, // GeoPoint
          rotation: data.playerRotation, // List<float>
        };
      })
      .filter((player) => player !== null); // Null olanları listeden çıkar

    // --- YENİ LOGLAMA ADIMI ---
    // Filtrelemeden sonra istemciye kaç oyuncu gönderdiğimizi görelim.
    logger.info(`Returning ${players.length} valid ghost players to client.`);
    // -------------------------

    return {success: true, players: players};
  } catch (error) {
    logger.error("Error in getGhostPlayers:", {errorDetails: error});
    throw new HttpsError("internal", "Hayalet oyuncu verisi çekilemedi.");
  }
});
